using InertiaCore.Constants;
using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Props;

/// <summary>
/// A prop for infinite scroll patterns with pagination metadata.
/// Wraps a value in a named key and attaches scroll position metadata.
/// </summary>
public class ScrollProp<T> : IInertiaProp, IDeferrable, IMergeable
{
    private readonly object? _value;
    private readonly string _wrapper;
    private readonly IProvidesScrollMetadata? _metadataProvider;
    private readonly DeferBehavior _defer = new();
    private readonly MergeBehavior _merge = new();

    /// <summary>
    /// Wraps a raw value with optional scroll metadata.
    /// </summary>
    public ScrollProp(T? value, string wrapper = "data", IProvidesScrollMetadata? metadataProvider = null)
        : this((object?)value, wrapper, metadataProvider)
    {
    }

    /// <summary>
    /// Wraps a synchronous callback with optional scroll metadata.
    /// </summary>
    public ScrollProp(Func<T?> callback, string wrapper = "data", IProvidesScrollMetadata? metadataProvider = null)
        : this((object?)callback, wrapper, metadataProvider)
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback with optional scroll metadata.
    /// </summary>
    public ScrollProp(Func<Task<T?>> callback, string wrapper = "data", IProvidesScrollMetadata? metadataProvider = null)
        : this((object?)callback, wrapper, metadataProvider)
    {
    }

    /// <summary>
    /// Wraps a synchronous callback with service provider access and optional scroll metadata.
    /// </summary>
    public ScrollProp(Func<IServiceProvider, T?> callback, string wrapper = "data", IProvidesScrollMetadata? metadataProvider = null)
        : this((object?)callback, wrapper, metadataProvider)
    {
    }

    private ScrollProp(object? value, string wrapper, IProvidesScrollMetadata? metadataProvider)
    {
        _value = value;
        _wrapper = wrapper;
        _metadataProvider = metadataProvider;
        _merge.EnableMerge();
    }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services)
    {
        var value = _value switch
        {
            Func<IServiceProvider, Task<T?>> f => await f(services),
            Func<IServiceProvider, T?> f => f(services),
            Func<Task<T?>> f => await f(),
            Func<T?> f => f(),
            _ => _value,
        };

        var result = new Dictionary<string, object?>
        {
            [_wrapper] = value,
        };

        var metadata = ResolveMetadata(value);
        if (metadata != null)
        {
            foreach (var (key, val) in metadata)
            {
                result[key] = val;
            }
        }

        return result;
    }

    /// <summary>
    /// The deferral configuration for this prop.
    /// </summary>
    public DeferBehavior Defer => _defer;

    /// <summary>
    /// The merge configuration for this prop.
    /// </summary>
    public MergeBehavior Merge => _merge;

    /// <summary>
    /// Configures merge intent from the request's X-Inertia-Infinite-Scroll-Merge-Intent header.
    /// </summary>
    public void ConfigureMergeIntent(HttpContext httpContext)
    {
        var intent = httpContext.Request.Headers[InertiaHeaders.InfiniteScrollMergeIntent]
            .FirstOrDefault();

        if (intent == "prepend")
        {
            _merge.Prepend();
        }
        else
        {
            _merge.Append();
        }
    }

    // -- Fluent API --

    /// <summary>
    /// Enables deferral for this scroll prop.
    /// </summary>
    public ScrollProp<T> WithDefer(string? group = null)
    {
        _defer.Defer(group);
        return this;
    }

    private Dictionary<string, object?>? ResolveMetadata(object? value)
    {
        var provider = _metadataProvider ?? value as IProvidesScrollMetadata;
        if (provider == null)
        {
            return null;
        }

        return new Dictionary<string, object?>
        {
            ["page"] = provider.GetPageName(),
            ["prevPage"] = provider.GetPreviousPage(),
            ["nextPage"] = provider.GetNextPage(),
            ["currentPage"] = provider.GetCurrentPage(),
        };
    }
}
