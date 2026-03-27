using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A typed prop with deferred loading, excluded from the initial response and loaded via partial reload.
/// </summary>
public class DeferProp<T> : IInertiaProp, IIgnoreFirstLoad, IDeferrable, IMergeable, IOnceable, ILiveProp, IFallbackProp, ITimedProp
{
    private readonly Func<IServiceProvider, Task<object?>> _callback;
    private readonly DeferBehavior _defer = new();
    private readonly MergeBehavior _merge = new();
    private readonly OnceBehavior _once = new();
    private readonly LiveBehavior _live = new();
    private readonly FallbackBehavior _fallback = new();
    private readonly TimedBehavior _timed = new();

    /// <summary>Wraps a synchronous typed callback.</summary>
    public DeferProp(Func<T?> callback, string? group = null)
    { _callback = _ => Task.FromResult<object?>(callback()); _defer.Defer(group); }
    /// <summary>Wraps an asynchronous typed callback.</summary>
    public DeferProp(Func<Task<T?>> callback, string? group = null)
    { _callback = async _ => (object?)await callback(); _defer.Defer(group); }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public DeferProp(Func<IServiceProvider, T?> callback, string? group = null)
    { _callback = sp => Task.FromResult<object?>(callback(sp)); _defer.Defer(group); }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public DeferProp(Func<IServiceProvider, Task<T?>> callback, string? group = null)
    { _callback = async sp => (object?)await callback(sp); _defer.Defer(group); }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services) => await _callback(services);

    /// <summary>The deferral configuration.</summary>
    public DeferBehavior Defer => _defer;
    /// <summary>The merge configuration.</summary>
    public MergeBehavior Merge => _merge;
    /// <summary>The once-resolution configuration.</summary>
    public OnceBehavior Once => _once;

    /// <summary>Enables shallow merge.</summary>
    public DeferProp<T> WithMerge() { _merge.EnableMerge(); return this; }
    /// <summary>Enables deep merge.</summary>
    public DeferProp<T> WithDeepMerge() { _merge.EnableDeepMerge(); return this; }
    /// <summary>Configures append.</summary>
    public DeferProp<T> Append(string? path = null, string? matchOn = null) { _merge.Append(path, matchOn); return this; }
    /// <summary>Configures prepend.</summary>
    public DeferProp<T> Prepend(string? path = null, string? matchOn = null) { _merge.Prepend(path, matchOn); return this; }
    /// <summary>Enables once-resolution.</summary>
    public DeferProp<T> OnlyOnce(string? key = null) { _once.EnableOnce(); if (key != null) _once.SetKey(key); return this; }
    /// <summary>Sets a custom cache key.</summary>
    public DeferProp<T> As(string key) { _once.SetKey(key); return this; }
    /// <summary>Forces re-resolution.</summary>
    public DeferProp<T> Fresh(bool value = true) { _once.SetRefresh(value); return this; }
    /// <summary>Sets a TTL.</summary>
    public DeferProp<T> Until(TimeSpan ttl) { _once.SetTtl(ttl); return this; }
    /// <summary>The live update configuration.</summary>
    public LiveBehavior Live => _live;
    /// <summary>Enables real-time updates via SignalR.</summary>
    public DeferProp<T> WithLive(string? channel = null) { _live.Enable(channel); return this; }
    /// <summary>The fallback configuration.</summary>
    public FallbackBehavior Fallback => _fallback;
    /// <summary>Sets a fallback value for when the prop is not included.</summary>
    public DeferProp<T> WithFallback(object? value) { _fallback.SetFallback(value); return this; }
    /// <summary>The timed refresh configuration.</summary>
    public TimedBehavior Timed => _timed;
    /// <summary>Configures the prop to refresh at a fixed interval.</summary>
    public DeferProp<T> RefreshEvery(TimeSpan interval) { _timed.SetInterval(interval); return this; }
}

/// <summary>
/// A prop with deferred loading, excluded from the initial response and loaded via partial reload.
/// </summary>
public sealed class DeferProp : DeferProp<object?>
{
    /// <summary>Wraps a synchronous callback.</summary>
    public DeferProp(Func<object?> callback, string? group = null) : base(callback, group) { }
    /// <summary>Wraps an asynchronous callback.</summary>
    public DeferProp(Func<Task<object?>> callback, string? group = null) : base(callback, group) { }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public DeferProp(Func<IServiceProvider, object?> callback, string? group = null) : base(callback, group) { }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public DeferProp(Func<IServiceProvider, Task<object?>> callback, string? group = null) : base(callback, group) { }
}
