using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.SignalR;

/// <summary>
/// Extension methods for registering Inertia SignalR services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Inertia SignalR real-time prop broadcasting services.
    /// </summary>
    public static IServiceCollection AddInertiaSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IInertiaBroadcaster, InertiaBroadcaster>();
        return services;
    }

    /// <summary>
    /// Maps the Inertia SignalR hub endpoint.
    /// </summary>
    public static IEndpointConventionBuilder MapInertiaHub(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/inertia-hub")
    {
        return endpoints.MapHub<InertiaHub>(pattern);
    }
}
