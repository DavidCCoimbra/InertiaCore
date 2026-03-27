using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A typed prop excluded from the initial page load but included when explicitly requested.
/// </summary>
public class OptionalProp<T> : IInertiaProp, IIgnoreFirstLoad, IOnceable, ILiveProp, IFallbackProp, ITimedProp
{
    private readonly Func<IServiceProvider, Task<object?>> _callback;
    private readonly OnceBehavior _once = new();
    private readonly LiveBehavior _live = new();
    private readonly FallbackBehavior _fallback = new();
    private readonly TimedBehavior _timed = new();

    /// <summary>Wraps a synchronous typed callback.</summary>
    public OptionalProp(Func<T?> callback) { _callback = _ => Task.FromResult<object?>(callback()); }
    /// <summary>Wraps an asynchronous typed callback.</summary>
    public OptionalProp(Func<Task<T?>> callback) { _callback = async _ => (object?)await callback(); }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public OptionalProp(Func<IServiceProvider, T?> callback) { _callback = sp => Task.FromResult<object?>(callback(sp)); }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public OptionalProp(Func<IServiceProvider, Task<T?>> callback) { _callback = async sp => (object?)await callback(sp); }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services) => await _callback(services);

    /// <summary>The once-resolution configuration.</summary>
    public OnceBehavior Once => _once;

    /// <summary>Enables once-resolution.</summary>
    public OptionalProp<T> OnlyOnce(string? key = null) { _once.EnableOnce(); if (key != null) _once.SetKey(key); return this; }
    /// <summary>Sets a custom cache key.</summary>
    public OptionalProp<T> As(string key) { _once.SetKey(key); return this; }
    /// <summary>Forces re-resolution.</summary>
    public OptionalProp<T> Fresh(bool value = true) { _once.SetRefresh(value); return this; }
    /// <summary>Sets a TTL.</summary>
    public OptionalProp<T> Until(TimeSpan ttl) { _once.SetTtl(ttl); return this; }
    /// <summary>The live update configuration.</summary>
    public LiveBehavior Live => _live;
    /// <summary>Enables real-time updates via SignalR.</summary>
    public OptionalProp<T> WithLive(string? channel = null) { _live.Enable(channel); return this; }
    /// <summary>The fallback configuration.</summary>
    public FallbackBehavior Fallback => _fallback;
    /// <summary>Sets a fallback value for when the prop is not included.</summary>
    public OptionalProp<T> WithFallback(object? value) { _fallback.SetFallback(value); return this; }
    /// <summary>The timed refresh configuration.</summary>
    public TimedBehavior Timed => _timed;
    /// <summary>Configures the prop to refresh at a fixed interval.</summary>
    public OptionalProp<T> RefreshEvery(TimeSpan interval) { _timed.SetInterval(interval); return this; }
}

/// <summary>
/// A prop excluded from the initial page load but included when explicitly requested.
/// </summary>
public sealed class OptionalProp : OptionalProp<object?>
{
    /// <summary>Wraps a synchronous callback.</summary>
    public OptionalProp(Func<object?> callback) : base(callback) { }
    /// <summary>Wraps an asynchronous callback.</summary>
    public OptionalProp(Func<Task<object?>> callback) : base(callback) { }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public OptionalProp(Func<IServiceProvider, object?> callback) : base(callback) { }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public OptionalProp(Func<IServiceProvider, Task<object?>> callback) : base(callback) { }
}
