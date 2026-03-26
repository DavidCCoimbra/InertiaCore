using InertiaCore.Core;
using InertiaCore.Filters;
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

    /// <summary>
    /// Adds an Inertia endpoint filter that configures the factory for matched routes.
    /// Useful for sharing props or setting version on specific route groups.
    /// </summary>
    public static RouteHandlerBuilder AddInertiaFilter(
        this RouteHandlerBuilder builder,
        Action<IInertiaResponseFactory> configure)
    {
        return builder.AddEndpointFilter(new InertiaEndpointFilter(configure));
    }

    /// <summary>
    /// Adds an Inertia endpoint filter to a route group.
    /// </summary>
    public static RouteGroupBuilder AddInertiaFilter(
        this RouteGroupBuilder builder,
        Action<IInertiaResponseFactory> configure)
    {
        return builder.AddEndpointFilter(new InertiaEndpointFilter(configure));
    }

    /// <summary>
    /// Adds automatic validation to this endpoint. If validation fails on an Inertia request,
    /// redirects back with errors. Non-Inertia requests get standard ValidationProblem responses.
    /// </summary>
    public static RouteHandlerBuilder AddInertiaValidation(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<InertiaValidationFilter>();
    }

    /// <summary>
    /// Adds automatic validation to all endpoints in this route group.
    /// </summary>
    public static RouteGroupBuilder AddInertiaValidation(this RouteGroupBuilder builder)
    {
        return builder.AddEndpointFilter<InertiaValidationFilter>();
    }
}
