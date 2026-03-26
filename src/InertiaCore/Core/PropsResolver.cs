using InertiaCore.Constants;
using InertiaCore.Context;
using InertiaCore.Contracts;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Core;

/// <summary>
/// Resolves the props tree by merging shared and page props, filtering by partial reload
/// headers, resolving IInertiaProp instances and closures, and collecting metadata.
/// </summary>
public sealed class PropsResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpContext? _httpContext;
    private readonly string? _component;

    private readonly bool _isPartial;
    private readonly PropsPathMatcher _pathMatcher;
    private readonly HashSet<string> _resetProps;
    private readonly PropMetadataCollector _metadata = new();

    /// <summary>
    /// Creates a resolver without HTTP context (for unit testing and non-HTTP scenarios).
    /// </summary>
    public PropsResolver(IServiceProvider serviceProvider)
        : this(serviceProvider, request: null, component: null)
    {
    }

    /// <summary>
    /// Creates a resolver with HTTP context for partial reload support.
    /// </summary>
    public PropsResolver(IServiceProvider serviceProvider, HttpRequest? request, string? component)
    {
        _serviceProvider = serviceProvider;
        _httpContext = request?.HttpContext;
        _component = component;

        var partialComponent = request?.Headers[InertiaHeaders.PartialComponent].FirstOrDefault();
        _isPartial = !string.IsNullOrEmpty(partialComponent) && partialComponent == component;

        _pathMatcher = new PropsPathMatcher(
            PropsPathMatcher.ParseHeader(request, InertiaHeaders.PartialData),
            PropsPathMatcher.ParseHeader(request, InertiaHeaders.PartialExcept));
        _resetProps = PropsPathMatcher.ParseHeader(request, InertiaHeaders.Reset);
    }

    /// <summary>
    /// Merges shared and page props, resolves all values, filters by partial reload headers,
    /// and returns the resolved dictionary with metadata.
    /// </summary>
    public async Task<(Dictionary<string, object?> Props, Dictionary<string, object?> Metadata)> ResolveAsync(
        Dictionary<string, object?> sharedProps,
        Dictionary<string, object?> pageProps)
    {
        var merged = new Dictionary<string, object?>(sharedProps);
        _metadata.TrackSharedKeys(sharedProps.Keys);

        foreach (var (key, value) in pageProps)
        {
            merged[key] = value;
        }

        merged = ResolveProviders(merged);
        merged = ExpandDotNotation(merged);

        var resolved = await ResolvePropsAsync(merged, prefix: "").ConfigureAwait(false);
        return (resolved, _metadata.Build());
    }

    private async Task<Dictionary<string, object?>> ResolvePropsAsync(
        Dictionary<string, object?> props,
        string prefix)
    {
        var resolved = new Dictionary<string, object?>();

        foreach (var (key, value) in props)
        {
            var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

            if (!ShouldInclude(path, value))
            {
                continue;
            }

            if (ShouldExcludeFromInitialResponse(path, value))
            {
                continue;
            }

            resolved[key] = await ResolveValueAsync(path, value).ConfigureAwait(false);
        }

        return resolved;
    }

    private bool ShouldInclude(string path, object? value)
    {
        if (!_isPartial)
        {
            return true;
        }

        if (value is IAlwaysIncluded)
        {
            return true;
        }

        if (_pathMatcher.HasOnlyFilter)
        {
            return _pathMatcher.MatchesOnly(path) || _pathMatcher.LeadsToOnly(path);
        }

        if (_pathMatcher.HasExceptFilter)
        {
            return !_pathMatcher.MatchesExcept(path);
        }

        return true;
    }

    private bool ShouldExcludeFromInitialResponse(string path, object? value)
    {
        if (_isPartial)
        {
            return false;
        }

        if (value is not IIgnoreFirstLoad)
        {
            return false;
        }

        CollectExcludedPropMetadata(path, value);
        return true;
    }

    private void CollectExcludedPropMetadata(string path, object? value)
    {
        if (value is IDeferrable deferrable && deferrable.Defer.ShouldDefer())
        {
            _metadata.AddDeferred(deferrable.Defer.Group(), path);

            if (value is IMergeable mergeable && mergeable.Merge.ShouldMerge()
                && !_resetProps.Contains(path))
            {
                _metadata.AddMerge(path);
            }
        }

        CollectOnceMetadata(path, value);
    }

    private void CollectMetadata(string path, object? value)
    {
        CollectMergeMetadata(path, value);
        CollectOnceMetadata(path, value);
    }

    private void CollectMergeMetadata(string path, object? value)
    {
        if (value is not IMergeable mergeable)
        {
            return;
        }

        if (!mergeable.Merge.ShouldMerge() || _resetProps.Contains(path))
        {
            return;
        }

        if (mergeable.Merge.ShouldDeepMerge())
        {
            _metadata.AddDeepMerge(path);
        }
        else if (mergeable.Merge.ShouldPrependAtRoot())
        {
            _metadata.AddPrepend(path);
        }
        else
        {
            _metadata.AddMerge(path);
        }

        var matchesOn = mergeable.Merge.MatchesOn();
        if (matchesOn.Length > 0)
        {
            _metadata.AddMatchOn(matchesOn);
        }
    }

    private void CollectOnceMetadata(string path, object? value)
    {
        if (value is not IOnceable onceable)
        {
            return;
        }

        if (!onceable.Once.ShouldResolveOnce())
        {
            return;
        }

        _metadata.AddOnce(path, onceable.Once.ExpiresAt());
    }

    private async Task<object?> ResolveValueAsync(string path, object? value)
    {
        var resolved = await ResolveRawValueAsync(value, path).ConfigureAwait(false);

        resolved = await UnwrapNestedPropAsync(path, value, resolved).ConfigureAwait(false);

        CollectMetadata(path, value);

        if (resolved is Dictionary<string, object?> resolvedDict && value is not Dictionary<string, object?>)
        {
            return await ResolvePropsAsync(resolvedDict, path).ConfigureAwait(false);
        }

        return resolved;
    }

    private async Task<object?> ResolveRawValueAsync(object? value, string path)
    {
        return value switch
        {
            IInertiaProp prop => await prop.ResolveAsync(_serviceProvider),
            IProvidesInertiaProperty provider when _httpContext != null =>
                provider.ToInertiaProperty(new PropertyContext(path, [], _httpContext)),
            Func<IServiceProvider, Task<object?>> f => await f(_serviceProvider),
            Func<IServiceProvider, object?> f => f(_serviceProvider),
            Func<Task<object?>> f => await f(),
            Func<object?> f => f(),
            Dictionary<string, object?> nested => await ResolvePropsAsync(nested, path),
            _ => value,
        };
    }

    private async Task<object?> UnwrapNestedPropAsync(string path, object? originalValue, object? resolved)
    {
        if (resolved is not IInertiaProp nestedProp || originalValue is IInertiaProp)
        {
            return resolved;
        }

        if (nestedProp is IIgnoreFirstLoad && !_isPartial)
        {
            CollectExcludedPropMetadata(path, nestedProp);
            return null;
        }

        return await nestedProp.ResolveAsync(_serviceProvider).ConfigureAwait(false);
    }

    // -- Providers and dot-notation --

    private Dictionary<string, object?> ResolveProviders(Dictionary<string, object?> props)
    {
        if (_httpContext == null || _component == null)
        {
            return props;
        }

        var result = new Dictionary<string, object?>();
        var renderContext = new RenderContext(_component, _httpContext);

        foreach (var (key, value) in props)
        {
            if (value is IProvidesInertiaProperties provider)
            {
                foreach (var kvp in provider.ToInertiaProperties(renderContext))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static Dictionary<string, object?> ExpandDotNotation(Dictionary<string, object?> props)
    {
        var dotKeys = props.Keys.Where(k => k.Contains('.')).ToList();
        if (dotKeys.Count == 0)
        {
            return props;
        }

        var result = new Dictionary<string, object?>(props);
        foreach (var dotKey in dotKeys)
        {
            result.Remove(dotKey);
            SetNestedValue(result, dotKey.Split('.'), props[dotKey]);
        }

        return result;
    }

    private static void SetNestedValue(Dictionary<string, object?> dict, string[] segments, object? value)
    {
        var current = dict;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (!current.TryGetValue(segment, out var existing) || existing is not Dictionary<string, object?> nested)
            {
                nested = new Dictionary<string, object?>();
                current[segment] = nested;
            }

            current = nested;
        }

        current[segments[^1]] = value;
    }
}
