using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A prop with deferred loading, excluded from the initial response and loaded via partial reload.
/// </summary>
public sealed class DeferProp : IInertiaProp, IIgnoreFirstLoad, IDeferrable, IMergeable, IOnceable
{
    private readonly Func<IServiceProvider, Task<object?>> _callback;
    private readonly DeferBehavior _defer = new();
    private readonly MergeBehavior _merge = new();
    private readonly OnceBehavior _once = new();

    /// <summary>
    /// Wraps a synchronous callback with a default group.
    /// </summary>
    public DeferProp(Func<object?> callback, string? group = null)
        : this(_ => Task.FromResult(callback()), group)
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback with a default group.
    /// </summary>
    public DeferProp(Func<Task<object?>> callback, string? group = null)
        : this(_ => callback(), group)
    {
    }

    /// <summary>
    /// Wraps a synchronous callback with service provider access and a default group.
    /// </summary>
    public DeferProp(Func<IServiceProvider, object?> callback, string? group = null)
        : this(sp => Task.FromResult(callback(sp)), group)
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback with service provider access and a default group.
    /// </summary>
    public DeferProp(Func<IServiceProvider, Task<object?>> callback, string? group = null)
    {
        _callback = callback;
        _defer.Defer(group);
    }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services) =>
        await _callback(services);

    /// <summary>
    /// The deferral configuration for this prop.
    /// </summary>
    public DeferBehavior Defer => _defer;

    /// <summary>
    /// The merge configuration for this prop.
    /// </summary>
    public MergeBehavior Merge => _merge;

    /// <summary>
    /// The once-resolution configuration for this prop.
    /// </summary>
    public OnceBehavior Once => _once;

    // -- Merge fluent API --

    /// <summary>
    /// Enables shallow merge for this prop.
    /// </summary>
    public DeferProp WithMerge()
    {
        _merge.EnableMerge();
        return this;
    }

    /// <summary>
    /// Enables deep merge for this prop.
    /// </summary>
    public DeferProp WithDeepMerge()
    {
        _merge.EnableDeepMerge();
        return this;
    }

    /// <summary>
    /// Configures append at root or at a specific path.
    /// </summary>
    public DeferProp Append(string? path = null, string? matchOn = null)
    {
        _merge.Append(path, matchOn);
        return this;
    }

    /// <summary>
    /// Configures prepend at root or at a specific path.
    /// </summary>
    public DeferProp Prepend(string? path = null, string? matchOn = null)
    {
        _merge.Prepend(path, matchOn);
        return this;
    }

    // -- Once fluent API --

    /// <summary>
    /// Enables once-resolution for this prop.
    /// </summary>
    public DeferProp OnlyOnce(string? key = null)
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
    public DeferProp As(string key)
    {
        _once.SetKey(key);
        return this;
    }

    /// <summary>
    /// Forces re-resolution even if cached.
    /// </summary>
    public DeferProp Fresh(bool value = true)
    {
        _once.SetRefresh(value);
        return this;
    }

    /// <summary>
    /// Sets a TTL for the cached value.
    /// </summary>
    public DeferProp Until(TimeSpan ttl)
    {
        _once.SetTtl(ttl);
        return this;
    }
}
