using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.EmbeddedV8;

/// <summary>
/// Extension methods for registering EmbeddedV8-specific endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a POST endpoint that triggers a V8 engine pool reload.
    /// Restricted to localhost connections only for security.
    /// Call this from a Vite post-build hook to signal that the SSR bundle is ready.
    /// </summary>
    public static IEndpointConventionBuilder MapInertiaV8Reload(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/inertia/ssr-reload")
    {
        return endpoints.MapPost(pattern, async (HttpContext context) =>
        {
            // Only allow reload from localhost — prevents DoS from external requests
            if (!IPAddress.IsLoopback(context.Connection.RemoteIpAddress ?? IPAddress.None))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            var pool = context.RequestServices.GetRequiredService<V8EnginePool>();
            await pool.TriggerReloadAsync();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"status\":\"reloaded\"}");
        });
    }
}
