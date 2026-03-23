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
}
