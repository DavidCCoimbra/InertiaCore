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
    /// Adds streaming SSR middleware. Flushes the HTML shell immediately,
    /// then streams SSR content and hydration data. Reduces TTFB by 3-6x.
    /// Must be registered before UseInertia().
    /// </summary>
    public static IApplicationBuilder UseInertiaStreaming(this IApplicationBuilder app) =>
        app.UseMiddleware<StreamingSsrMiddleware>();

    /// <summary>
    /// Adds the Inertia developer exception page. Returns error details as JSON for
    /// Inertia XHR requests instead of breaking the SPA with an HTML error page.
    /// Only active in Development environment.
    /// </summary>
    public static IApplicationBuilder UseInertiaDeveloperExceptionPage(this IApplicationBuilder app) =>
        app.UseMiddleware<InertiaExceptionMiddleware>();

    /// <summary>
    /// Adds history encryption middleware. All Inertia responses passing through
    /// will have <c>encryptHistory: true</c> in the page object.
    /// Can be applied globally or to specific route groups via UseWhen().
    /// </summary>
    public static IApplicationBuilder UseInertiaEncryptHistory(this IApplicationBuilder app) =>
        app.UseMiddleware<EncryptHistoryMiddleware>();
}
