using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A typed prop that merges with existing client-side data instead of replacing it.
/// </summary>
public class MergeProp<T> : IInertiaProp, IMergeable, IOnceable, ILiveProp, IFallbackProp, ITimedProp
{
    private readonly object? _value;
    private readonly MergeBehavior _merge = new();
    private readonly OnceBehavior _once = new();
    private readonly LiveBehavior _live = new();
    private readonly FallbackBehavior _fallback = new();
    private readonly TimedBehavior _timed = new();

    /// <summary>Wraps a raw typed value with merge enabled.</summary>
    public MergeProp(T? value) : this((object?)value) { }
    /// <summary>Wraps a synchronous typed callback.</summary>
    public MergeProp(Func<T?> callback) : this((object?)callback) { }
    /// <summary>Wraps an asynchronous typed callback.</summary>
    public MergeProp(Func<Task<T?>> callback) : this((object?)callback) { }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public MergeProp(Func<IServiceProvider, T?> callback) : this((object?)callback) { }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public MergeProp(Func<IServiceProvider, Task<T?>> callback) : this((object?)callback) { }

    /// <summary>Internal canonical constructor.</summary>
    protected MergeProp(object? value)
    {
        _value = value;
        _merge.EnableMerge();
    }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services)
    {
        return _value switch
        {
            Func<IServiceProvider, Task<T?>> f => await f(services),
            Func<IServiceProvider, T?> f => f(services),
            Func<Task<T?>> f => await f(),
            Func<T?> f => f(),
            _ => _value,
        };
    }

    /// <summary>The merge configuration.</summary>
    public MergeBehavior Merge => _merge;
    /// <summary>The once-resolution configuration.</summary>
    public OnceBehavior Once => _once;

    /// <summary>Enables deep merge.</summary>
    public MergeProp<T> WithDeepMerge() { _merge.EnableDeepMerge(); return this; }
    /// <summary>Configures append.</summary>
    public MergeProp<T> Append(string? path = null, string? matchOn = null) { _merge.Append(path, matchOn); return this; }
    /// <summary>Configures prepend.</summary>
    public MergeProp<T> Prepend(string? path = null, string? matchOn = null) { _merge.Prepend(path, matchOn); return this; }
    /// <summary>Enables once-resolution.</summary>
    public MergeProp<T> OnlyOnce(string? key = null) { _once.EnableOnce(); if (key != null) _once.SetKey(key); return this; }
    /// <summary>Sets a custom cache key.</summary>
    public MergeProp<T> As(string key) { _once.SetKey(key); return this; }
    /// <summary>Forces re-resolution.</summary>
    public MergeProp<T> Fresh(bool value = true) { _once.SetRefresh(value); return this; }
    /// <summary>Sets a TTL.</summary>
    public MergeProp<T> Until(TimeSpan ttl) { _once.SetTtl(ttl); return this; }
    /// <summary>The live update configuration.</summary>
    public LiveBehavior Live => _live;
    /// <summary>Enables real-time updates via SignalR.</summary>
    public MergeProp<T> WithLive(string? channel = null) { _live.Enable(channel); return this; }
    /// <summary>The fallback configuration.</summary>
    public FallbackBehavior Fallback => _fallback;
    /// <summary>Sets a fallback value for when the prop is not included.</summary>
    public MergeProp<T> WithFallback(object? value) { _fallback.SetFallback(value); return this; }
    /// <summary>The timed refresh configuration.</summary>
    public TimedBehavior Timed => _timed;
    /// <summary>Configures the prop to refresh at a fixed interval.</summary>
    public MergeProp<T> RefreshEvery(TimeSpan interval) { _timed.SetInterval(interval); return this; }
}

/// <summary>
/// A prop that merges with existing client-side data instead of replacing it.
/// </summary>
public sealed class MergeProp : MergeProp<object?>
{
    /// <summary>Wraps a raw value with merge enabled.</summary>
    public MergeProp(object? value) : base(value) { }
    /// <summary>Wraps a synchronous callback.</summary>
    public MergeProp(Func<object?> callback) : base(callback) { }
    /// <summary>Wraps an asynchronous callback.</summary>
    public MergeProp(Func<Task<object?>> callback) : base(callback) { }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public MergeProp(Func<IServiceProvider, object?> callback) : base(callback) { }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public MergeProp(Func<IServiceProvider, Task<object?>> callback) : base(callback) { }
}
