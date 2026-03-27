using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A typed prop that is resolved once and excluded on subsequent requests.
/// </summary>
public class OnceProp<T> : IInertiaProp, IIgnoreFirstLoad, IOnceable, ILiveProp, IFallbackProp, ITimedProp
{
    private readonly Func<IServiceProvider, Task<object?>> _callback;
    private readonly OnceBehavior _once = new();
    private readonly LiveBehavior _live = new();
    private readonly FallbackBehavior _fallback = new();
    private readonly TimedBehavior _timed = new();

    /// <summary>Wraps a synchronous typed callback.</summary>
    public OnceProp(Func<T?> callback)
    { _callback = _ => Task.FromResult<object?>(callback()); _once.EnableOnce(); }
    /// <summary>Wraps an asynchronous typed callback.</summary>
    public OnceProp(Func<Task<T?>> callback)
    { _callback = async _ => (object?)await callback(); _once.EnableOnce(); }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public OnceProp(Func<IServiceProvider, T?> callback)
    { _callback = sp => Task.FromResult<object?>(callback(sp)); _once.EnableOnce(); }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public OnceProp(Func<IServiceProvider, Task<T?>> callback)
    { _callback = async sp => (object?)await callback(sp); _once.EnableOnce(); }

    /// <summary>Internal canonical constructor.</summary>
    protected OnceProp(Func<IServiceProvider, Task<object?>> callback)
    {
        _callback = callback;
        _once.EnableOnce();
    }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services) =>
        await _callback(services);

    /// <summary>The once-resolution configuration.</summary>
    public OnceBehavior Once => _once;

    /// <summary>Sets a custom cache key.</summary>
    public OnceProp<T> As(string key) { _once.SetKey(key); return this; }
    /// <summary>Forces re-resolution.</summary>
    public OnceProp<T> Fresh(bool value = true) { _once.SetRefresh(value); return this; }
    /// <summary>Sets a TTL.</summary>
    public OnceProp<T> Until(TimeSpan ttl) { _once.SetTtl(ttl); return this; }
    /// <summary>The live update configuration.</summary>
    public LiveBehavior Live => _live;
    /// <summary>Enables real-time updates via SignalR.</summary>
    public OnceProp<T> WithLive(string? channel = null) { _live.Enable(channel); return this; }
    /// <summary>The fallback configuration.</summary>
    public FallbackBehavior Fallback => _fallback;
    /// <summary>Sets a fallback value for when the prop is not included.</summary>
    public OnceProp<T> WithFallback(object? value) { _fallback.SetFallback(value); return this; }
    /// <summary>The timed refresh configuration.</summary>
    public TimedBehavior Timed => _timed;
    /// <summary>Configures the prop to refresh at a fixed interval.</summary>
    public OnceProp<T> RefreshEvery(TimeSpan interval) { _timed.SetInterval(interval); return this; }
}

/// <summary>
/// A prop that is resolved once and excluded on subsequent requests.
/// </summary>
public sealed class OnceProp : OnceProp<object?>
{
    /// <summary>Wraps a synchronous callback.</summary>
    public OnceProp(Func<object?> callback)
        : base(_ => Task.FromResult(callback())) { }
    /// <summary>Wraps an asynchronous callback.</summary>
    public OnceProp(Func<Task<object?>> callback)
        : base(_ => callback()) { }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public OnceProp(Func<IServiceProvider, object?> callback)
        : base(sp => Task.FromResult(callback(sp))) { }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public OnceProp(Func<IServiceProvider, Task<object?>> callback)
        : base(callback) { }
}
