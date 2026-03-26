using InertiaCore.Core;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Extensions;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> providing Inertia route shortcuts.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a GET endpoint that renders an Inertia component with no controller logic needed.
    /// </summary>
    public static IEndpointConventionBuilder MapInertia(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string component)
    {
        return endpoints.MapGet(pattern, (IInertiaResponseFactory inertia) =>
            inertia.Render(component));
    }

    /// <summary>
    /// Maps a GET endpoint that renders an Inertia component with static props.
    /// </summary>
    public static IEndpointConventionBuilder MapInertia(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string component,
        object props)
    {
        return endpoints.MapGet(pattern, (IInertiaResponseFactory inertia) =>
            inertia.Render(component, props));
    }

    /// <summary>
    /// Maps a GET endpoint that renders an Inertia component with a dictionary of props.
    /// </summary>
    public static IEndpointConventionBuilder MapInertia(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        string component,
        Dictionary<string, object?> props)
    {
        return endpoints.MapGet(pattern, (IInertiaResponseFactory inertia) =>
            inertia.Render(component, props));
    }

    /// <summary>
    /// Maps a health check endpoint for the SSR sidecar.
    /// </summary>
    public static IEndpointConventionBuilder MapInertiaSsrHealthCheck(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/health/ssr")
    {
        return endpoints.MapGet(pattern, async (HttpContext context) =>
        {
            var gateway = context.RequestServices.GetService<ISsrGateway>();
            if (gateway == null)
            {
                return Results.Ok(new { status = "disabled" });
            }

            var healthy = await gateway.IsHealthyAsync(context.RequestAborted);
            return healthy
                ? Results.Ok(new { status = "healthy" })
                : Results.Json(new { status = "unhealthy" }, statusCode: 503);
        });
    }
}
