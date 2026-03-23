using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A prop that is resolved once and excluded on subsequent requests.
/// </summary>
public class OnceProp : IInertiaProp, IIgnoreFirstLoad, IOnceable
{
    private readonly Func<IServiceProvider, Task<object?>> _callback;
    private readonly OnceBehavior _once = new();

    /// <summary>
    /// Wraps a synchronous callback.
    /// </summary>
    public OnceProp(Func<object?> callback)
        : this(_ => Task.FromResult(callback()))
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback.
    /// </summary>
    public OnceProp(Func<Task<object?>> callback)
        : this(_ => callback())
    {
    }

    /// <summary>
    /// Wraps a synchronous callback with service provider access.
    /// </summary>
    public OnceProp(Func<IServiceProvider, object?> callback)
        : this(sp => Task.FromResult(callback(sp)))
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback with service provider access.
    /// </summary>
    public OnceProp(Func<IServiceProvider, Task<object?>> callback)
    {
        _callback = callback;
        _once.EnableOnce();
    }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services) =>
        await _callback(services);

    /// <summary>
    /// The once-resolution configuration for this prop.
    /// </summary>
    public OnceBehavior Once => _once;

    /// <summary>
    /// Sets a custom cache key.
    /// </summary>
    public OnceProp As(string key)
    {
        _once.SetKey(key);
        return this;
    }

    /// <summary>
    /// Forces re-resolution even if cached.
    /// </summary>
    public OnceProp Fresh(bool value = true)
    {
        _once.SetRefresh(value);
        return this;
    }

    /// <summary>
    /// Sets a TTL for the cached value.
    /// </summary>
    public OnceProp Until(TimeSpan ttl)
    {
        _once.SetTtl(ttl);
        return this;
    }
}
