using Microsoft.AspNetCore.Http;

namespace InertiaCore.Contracts;

/// <summary>
/// Provides shared props for every Inertia response. Register via DI to automatically
/// merge props into every page render. Multiple providers are supported and merged in order.
/// </summary>
public interface ISharedPropsProvider
{
    /// <summary>
    /// Returns shared props for the current request.
    /// </summary>
    Dictionary<string, object?> GetSharedProps(HttpContext context);
}
