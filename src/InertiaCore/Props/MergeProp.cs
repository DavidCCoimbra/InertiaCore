using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A prop that merges with existing client-side data instead of replacing it.
/// </summary>
public sealed class MergeProp : IInertiaProp, IMergeable, IOnceable
{
    private readonly object? _value;
    private readonly MergeBehavior _merge = new();
    private readonly OnceBehavior _once = new();

    /// <summary>
    /// Wraps a raw value with merge enabled.
    /// </summary>
    public MergeProp(object? value)
    {
        _value = value;
        _merge.EnableMerge();
    }

    /// <summary>
    /// Wraps a synchronous callback with merge enabled.
    /// </summary>
    public MergeProp(Func<object?> callback)
        : this((object?)callback)
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback with merge enabled.
    /// </summary>
    public MergeProp(Func<Task<object?>> callback)
        : this((object?)callback)
    {
    }

    /// <summary>
    /// Wraps a synchronous callback with service provider access and merge enabled.
    /// </summary>
    public MergeProp(Func<IServiceProvider, object?> callback)
        : this((object?)callback)
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback with service provider access and merge enabled.
    /// </summary>
    public MergeProp(Func<IServiceProvider, Task<object?>> callback)
        : this((object?)callback)
    {
    }

    /// <inheritdoc />
    public Task<object?> ResolveAsync(IServiceProvider services) =>
        PropValueResolver.ResolveAsync(_value, services);

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
    /// Enables deep merge for this prop.
    /// </summary>
    public MergeProp WithDeepMerge()
    {
        _merge.EnableDeepMerge();
        return this;
    }

    /// <summary>
    /// Configures append at root or at a specific path.
    /// </summary>
    public MergeProp Append(string? path = null, string? matchOn = null)
    {
        _merge.Append(path, matchOn);
        return this;
    }

    /// <summary>
    /// Configures prepend at root or at a specific path.
    /// </summary>
    public MergeProp Prepend(string? path = null, string? matchOn = null)
    {
        _merge.Prepend(path, matchOn);
        return this;
    }

    // -- Once fluent API --

    /// <summary>
    /// Enables once-resolution for this prop.
    /// </summary>
    public MergeProp OnlyOnce(string? key = null)
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
    public MergeProp As(string key)
    {
        _once.SetKey(key);
        return this;
    }

    /// <summary>
    /// Forces re-resolution even if cached.
    /// </summary>
    public MergeProp Fresh(bool value = true)
    {
        _once.SetRefresh(value);
        return this;
    }

    /// <summary>
    /// Sets a TTL for the cached value.
    /// </summary>
    public MergeProp Until(TimeSpan ttl)
    {
        _once.SetTtl(ttl);
        return this;
    }
}
