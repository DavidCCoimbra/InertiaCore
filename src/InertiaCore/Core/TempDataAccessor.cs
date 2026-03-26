using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Core;

/// <summary>
/// Shared utility for accessing TempData from an HttpContext.
/// </summary>
internal static class TempDataAccessor
{
    /// <summary>
    /// Retrieves the TempData dictionary for the current request, or null if unavailable.
    /// </summary>
    public static ITempDataDictionary? GetTempData(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        var tempDataFactory = httpContext.RequestServices.GetService<ITempDataDictionaryFactory>();
        return tempDataFactory?.GetTempData(httpContext);
    }
}
