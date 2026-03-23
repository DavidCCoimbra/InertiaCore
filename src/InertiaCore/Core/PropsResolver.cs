using InertiaCore.Constants;
using InertiaCore.Contracts;
using InertiaCore.Props;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Core;

/// <summary>
/// Resolves the props tree by merging shared and page props, filtering by partial reload
/// headers, resolving IInertiaProp instances and closures, and collecting metadata.
/// </summary>
public class PropsResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpRequest? _request;

    private readonly bool _isPartial;
    private readonly HashSet<string> _only;
    private readonly HashSet<string> _except;
    private readonly HashSet<string> _resetProps;
    private readonly HashSet<string> _loadedOnceProps;

    private readonly List<Dictionary<string, object?>> _deferredProps = [];
    private readonly List<string> _mergeProps = [];
    private readonly List<string> _deepMergeProps = [];

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
        _request = request;

        var partialComponent = request?.Headers[InertiaHeaders.PartialComponent].FirstOrDefault();
        _isPartial = partialComponent != null && partialComponent == component;

        _only = ParseHeader(InertiaHeaders.PartialOnly);
        _except = ParseHeader(InertiaHeaders.PartialExcept);
        _resetProps = ParseHeader(InertiaHeaders.Reset);
        _loadedOnceProps = ParseHeader(InertiaHeaders.ExceptOnceProps);
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
        foreach (var (key, value) in pageProps)
        {
            merged[key] = value;
        }

        var resolved = await ResolvePropsAsync(merged, prefix: "");
        return (resolved, BuildMetadata());
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

            resolved[key] = await ResolveValueAsync(path, value);
        }

        return resolved;
    }

    private bool ShouldInclude(string path, object? value)
    {
        if (!_isPartial)
        {
            return true;
        }

        // AlwaysProp bypasses partial filtering
        if (value is AlwaysProp)
        {
            return true;
        }

        // If "only" is specified, use only/leadsToOnly matching
        if (_only.Count > 0)
        {
            return MatchesOnly(path) || LeadsToOnly(path);
        }

        // If "except" is specified, exclude matching paths
        if (_except.Count > 0)
        {
            return !MatchesExcept(path);
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
            var entry = new Dictionary<string, object?>
            {
                ["key"] = path,
                ["group"] = deferrable.Defer.Group(),
            };

            if (value is IMergeable mergeable && mergeable.Merge.ShouldMerge()
                && !_resetProps.Contains(path))
            {
                entry["merge"] = true;
            }

            _deferredProps.Add(entry);
        }

        // TODO: Once metadata collection will be added when the client protocol requires it
    }

    private void CollectMetadata(string path, object? value)
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
            _deepMergeProps.Add(path);
        }
        else
        {
            _mergeProps.Add(path);
        }
    }

    private async Task<object?> ResolveValueAsync(string path, object? value)
    {
        var resolved = await ResolveRawValueAsync(value, path);

        resolved = await UnwrapNestedPropAsync(path, value, resolved);

        CollectMetadata(path, value);

        if (resolved is Dictionary<string, object?> resolvedDict && value is not Dictionary<string, object?>)
        {
            return await ResolvePropsAsync(resolvedDict, path);
        }

        return resolved;
    }

    private async Task<object?> ResolveRawValueAsync(object? value, string path)
    {
        return value switch
        {
            IInertiaProp prop => await prop.ResolveAsync(_serviceProvider),
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

        return await nestedProp.ResolveAsync(_serviceProvider);
    }

    private Dictionary<string, object?> BuildMetadata()
    {
        var metadata = new Dictionary<string, object?>();

        if (_deferredProps.Count > 0)
        {
            metadata["deferredProps"] = _deferredProps;
        }

        if (_mergeProps.Count > 0)
        {
            metadata["mergeProps"] = _mergeProps;
        }

        if (_deepMergeProps.Count > 0)
        {
            metadata["deepMergeProps"] = _deepMergeProps;
        }

        return metadata;
    }

    // -- Partial reload matching --

    private bool MatchesOnly(string path)
    {
        return _only.Any(only =>
            path == only || path.StartsWith($"{only}.", StringComparison.Ordinal));
    }

    private bool LeadsToOnly(string path)
    {
        return _only.Any(only =>
            only.StartsWith($"{path}.", StringComparison.Ordinal));
    }

    private bool MatchesExcept(string path)
    {
        return _except.Any(except =>
            path == except || path.StartsWith($"{except}.", StringComparison.Ordinal));
    }

    private HashSet<string> ParseHeader(string headerName)
    {
        var value = _request?.Headers[headerName].FirstOrDefault();
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}
