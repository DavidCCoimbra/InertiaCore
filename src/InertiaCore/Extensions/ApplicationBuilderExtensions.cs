using InertiaCore.Middleware;
using Microsoft.AspNetCore.Builder;

namespace InertiaCore.Extensions;

/// <summary>
/// Extension methods for adding Inertia middleware to the pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the Inertia middleware to the request pipeline.
    /// </summary>
    public static IApplicationBuilder UseInertia(this IApplicationBuilder app) =>
        app.UseMiddleware<InertiaMiddleware>();

    /// <summary>
    /// Adds the Inertia developer exception page. Returns error details as JSON for
    /// Inertia XHR requests instead of breaking the SPA with an HTML error page.
    /// Only active in Development environment.
    /// </summary>
    public static IApplicationBuilder UseInertiaDeveloperExceptionPage(this IApplicationBuilder app) =>
        app.UseMiddleware<InertiaExceptionMiddleware>();
}
