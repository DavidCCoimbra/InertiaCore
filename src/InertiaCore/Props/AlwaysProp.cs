using InertiaCore.Contracts;

namespace InertiaCore.Props;

/// <summary>
/// A prop that is always included, even during partial reloads.
/// </summary>
public class AlwaysProp : IInertiaProp
{
    private readonly object? _value;

    /// <summary>
    /// Wraps a raw value.
    /// </summary>
    public AlwaysProp(object? value)
    {
        _value = value;
    }

    /// <summary>
    /// Wraps a synchronous callback.
    /// </summary>
    public AlwaysProp(Func<object?> callback)
        : this((object?)callback)
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback.
    /// </summary>
    public AlwaysProp(Func<Task<object?>> callback)
        : this((object?)callback)
    {
    }

    /// <summary>
    /// Wraps a synchronous callback with service provider access.
    /// </summary>
    public AlwaysProp(Func<IServiceProvider, object?> callback)
        : this((object?)callback)
    {
    }

    /// <summary>
    /// Wraps an asynchronous callback with service provider access.
    /// </summary>
    public AlwaysProp(Func<IServiceProvider, Task<object?>> callback)
        : this((object?)callback)
    {
    }

    /// <inheritdoc />
    public async Task<object?> ResolveAsync(IServiceProvider services)
    {
        return _value switch
        {
            Func<IServiceProvider, Task<object?>> f => await f(services),
            Func<IServiceProvider, object?> f => f(services),
            Func<Task<object?>> f => await f(),
            Func<object?> f => f(),
            _ => _value,
        };
    }
}
