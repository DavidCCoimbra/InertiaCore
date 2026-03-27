using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Vite.Extensions;

/// <summary>
/// Extension methods for registering Vite services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Vite asset resolution services to the service collection.
    /// </summary>
    public static IServiceCollection AddVite(
        this IServiceCollection services,
        Action<ViteOptions>? configure = null)
    {
        var builder = services.AddOptions<ViteOptions>()
            .BindConfiguration("Vite");

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        services.AddSingleton<IViteManifestReader, ViteManifestReader>();
        services.AddSingleton<IViteDevServerDetector, ViteDevServerDetector>();
        services.AddScoped<IViteAssetResolver, ViteAssetResolver>();

        return services;
    }
}
