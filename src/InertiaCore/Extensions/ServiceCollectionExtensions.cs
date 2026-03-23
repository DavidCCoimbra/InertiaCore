using InertiaCore.Configuration;
using InertiaCore.Core;
using InertiaCore.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Extensions;

/// <summary>
/// Extension methods for registering Inertia services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Inertia services in the DI container.
    /// </summary>
    public static IServiceCollection AddInertia(
        this IServiceCollection services,
        Action<InertiaOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddScoped<InertiaResponseFactory>();
        services.AddSingleton<InertiaMiddleware>();

        return services;
    }
}
