using InertiaCore.Constants;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Extensions;

/// <summary>
/// Extension methods for Inertia-related HTTP context operations.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Returns true if the request is an Inertia request (has the X-Inertia header).
    /// </summary>
    public static bool IsInertiaRequest(this HttpContext context) =>
        context.Request.Headers.ContainsKey(InertiaHeaders.Inertia);

    /// <summary>
    /// Returns the X-Inertia-Version header value, or null if not present.
    /// </summary>
    public static string? GetInertiaVersion(this HttpContext context) =>
        context.Request.Headers[InertiaHeaders.Version].FirstOrDefault();
}
