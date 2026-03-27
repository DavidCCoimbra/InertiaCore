using InertiaCore.Contracts;
using InertiaCore.Props.Behaviors;

namespace InertiaCore.Props;

/// <summary>
/// A typed prop that is always included, even during partial reloads.
/// </summary>
public class AlwaysProp<T> : IInertiaProp, IAlwaysIncluded, ILiveProp, IFallbackProp, ITimedProp
{
    private readonly object? _value;
    private readonly LiveBehavior _live = new();
    private readonly FallbackBehavior _fallback = new();
    private readonly TimedBehavior _timed = new();

    /// <summary>Wraps a raw typed value.</summary>
    public AlwaysProp(T? value) : this((object?)value) { }
    /// <summary>Wraps a synchronous typed callback.</summary>
    public AlwaysProp(Func<T?> callback) : this((object?)callback) { }
    /// <summary>Wraps an asynchronous typed callback.</summary>
    public AlwaysProp(Func<Task<T?>> callback) : this((object?)callback) { }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public AlwaysProp(Func<IServiceProvider, T?> callback) : this((object?)callback) { }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public AlwaysProp(Func<IServiceProvider, Task<T?>> callback) : this((object?)callback) { }

    /// <summary>Internal canonical constructor.</summary>
    protected AlwaysProp(object? value) => _value = value;

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

    /// <summary>The live update configuration.</summary>
    public LiveBehavior Live => _live;

    /// <summary>Enables real-time updates via SignalR on the specified channel.</summary>
    public AlwaysProp<T> WithLive(string? channel = null) { _live.Enable(channel); return this; }
    /// <summary>The fallback configuration.</summary>
    public FallbackBehavior Fallback => _fallback;
    /// <summary>Sets a fallback value for when the prop is not included.</summary>
    public AlwaysProp<T> WithFallback(object? value) { _fallback.SetFallback(value); return this; }
    /// <summary>The timed refresh configuration.</summary>
    public TimedBehavior Timed => _timed;
    /// <summary>Configures the prop to refresh at a fixed interval.</summary>
    public AlwaysProp<T> RefreshEvery(TimeSpan interval) { _timed.SetInterval(interval); return this; }
}

/// <summary>
/// A prop that is always included, even during partial reloads.
/// </summary>
public sealed class AlwaysProp : AlwaysProp<object?>
{
    /// <summary>Wraps a raw value.</summary>
    public AlwaysProp(object? value) : base(value) { }
    /// <summary>Wraps a synchronous callback.</summary>
    public AlwaysProp(Func<object?> callback) : base(callback) { }
    /// <summary>Wraps an asynchronous callback.</summary>
    public AlwaysProp(Func<Task<object?>> callback) : base(callback) { }
    /// <summary>Wraps a synchronous callback with service provider access.</summary>
    public AlwaysProp(Func<IServiceProvider, object?> callback) : base(callback) { }
    /// <summary>Wraps an asynchronous callback with service provider access.</summary>
    public AlwaysProp(Func<IServiceProvider, Task<object?>> callback) : base(callback) { }
}
