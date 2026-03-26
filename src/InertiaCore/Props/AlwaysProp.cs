using InertiaCore.Contracts;

namespace InertiaCore.Props;

/// <summary>
/// A typed prop that is always included, even during partial reloads.
/// </summary>
public class AlwaysProp<T> : IInertiaProp
{
    private readonly object? _value;

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
