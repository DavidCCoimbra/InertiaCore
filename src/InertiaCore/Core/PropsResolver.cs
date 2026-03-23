namespace InertiaCore.Core;

/// <summary>
/// Resolves the props tree by merging shared and page props, resolving closures and async values.
/// </summary>
public class PropsResolver(IServiceProvider serviceProvider)
{
    /// <summary>
    /// Merges shared and page props, resolves all callable values, and returns the resolved dictionary with empty metadata
    /// </summary>
    public async Task<(Dictionary<string, object?> Props, Dictionary<string, object?> Metadata)> ResolveAsync(
        Dictionary<string, object?> sharedProps,
        Dictionary<string, object?> pageProps)
    {
        var merged = new Dictionary<string, object?>(sharedProps);
        foreach (var (key, value) in pageProps)
        {
            merged[key] = value;
        }

        var resolved = await ResolvePropsAsync(merged);
        return (resolved, new Dictionary<string, object?>());
    }

    private async Task<Dictionary<string, object?>> ResolvePropsAsync(Dictionary<string, object?> props)
    {
        var resolved = new Dictionary<string, object?>();

        foreach (var (key, value) in props)
        {
            resolved[key] = await ResolveValueAsync(value);
        }

        return resolved;
    }

    private async Task<object?> ResolveValueAsync(object? value)
    {
        return value switch
        {
            Func<IServiceProvider, Task<object?>> asyncServiceFunc => await asyncServiceFunc(serviceProvider),
            Func<IServiceProvider, object?> serviceFunc => serviceFunc(serviceProvider),
            Func<Task<object?>> asyncFunc => await asyncFunc(),
            Func<object?> func => func(),
            Dictionary<string, object?> nested => await ResolvePropsAsync(nested),
            _ => value,
        };
    }
}
