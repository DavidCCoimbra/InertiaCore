namespace InertiaCore.Props;

/// <summary>
/// Resolves a prop value from raw values, synchronous/async callbacks, or DI-aware callbacks.
/// </summary>
internal static class PropValueResolver
{
    /// <summary>
    /// Resolves the prop value by invoking callbacks or returning the raw value.
    /// </summary>
    public static async Task<object?> ResolveAsync(object? value, IServiceProvider services)
    {
        return value switch
        {
            Func<IServiceProvider, Task<object?>> f => await f(services),
            Func<IServiceProvider, object?> f => f(services),
            Func<Task<object?>> f => await f(),
            Func<object?> f => f(),
            _ => value,
        };
    }
}
