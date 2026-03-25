using InertiaCore.Configuration;
using InertiaCore.Core;
using InertiaCore.Middleware;
using InertiaCore.Ssr;
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
        services.AddHttpContextAccessor();
        services.AddScoped<IInertiaFlashService, InertiaFlashService>();
        services.AddScoped<IInertiaErrorService, InertiaErrorService>();
        services.AddScoped<InertiaResponseFactory>();
        services.AddSingleton<InertiaMiddleware>();
        services.AddSingleton<EncryptHistoryMiddleware>();
        services.AddHttpClient<ISsrGateway, HttpSsrGateway>();

        return services;
    }
}
