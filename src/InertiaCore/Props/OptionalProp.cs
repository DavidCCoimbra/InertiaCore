using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A prop excluded from the initial page load but included when explicitly requested.
/// </summary>
public sealed class OptionalProp : IInertiaProp, IIgnoreFirstLoad, IOnceable
{
    private readonly Func<IServiceProvider, Task<object?>> _callback;
    private readonly OnceBehavior _once = new();

    /// <summary>
    /// Wraps a synchronous callback.
    /// </summary>
    public OptionalProp(Func<object?> callback)
        : this(_ => Task.FromResult(callback()))
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback.
    /// </summary>
    public OptionalProp(Func<Task<object?>> callback)
        : this(_ => callback())
    {
    }

    /// <summary>
    /// Wraps a synchronous callback with service provider access.
    /// </summary>
    public OptionalProp(Func<IServiceProvider, object?> callback)
        : this(sp => Task.FromResult(callback(sp)))
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback with service provider access.
    /// </summary>
    public OptionalProp(Func<IServiceProvider, Task<object?>> callback)
    {
        _callback = callback;
    }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services) =>
        await _callback(services);

    /// <summary>
    /// The once-resolution configuration for this prop.
    /// </summary>
    public OnceBehavior Once => _once;

    /// <summary>
    /// Enables once-resolution for this prop.
    /// </summary>
    public OptionalProp OnlyOnce(string? key = null)
    {
        _once.EnableOnce();
        if (key != null)
        {
            _once.SetKey(key);
        }

        return this;
    }

    /// <summary>
    /// Sets a custom cache key.
    /// </summary>
    public OptionalProp As(string key)
    {
        _once.SetKey(key);
        return this;
    }

    /// <summary>
    /// Forces re-resolution even if cached.
    /// </summary>
    public OptionalProp Fresh(bool value = true)
    {
        _once.SetRefresh(value);
        return this;
    }

    /// <summary>
    /// Sets a TTL for the cached value.
    /// </summary>
    public OptionalProp Until(TimeSpan ttl)
    {
        _once.SetTtl(ttl);
        return this;
    }
}
