using InertiaCore.Configuration;
using InertiaCore.Core;
using InertiaCore.Middleware;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<IValidateOptions<InertiaOptions>, InertiaOptionsValidator>();
        services.AddHttpContextAccessor();
        services.AddScoped<IInertiaFlashService, InertiaFlashService>();
        services.AddScoped<IInertiaErrorService, InertiaErrorService>();
        services.AddScoped<IInertiaResponseFactory, InertiaResponseFactory>();
        services.AddSingleton<InertiaMiddleware>();
        services.AddSingleton<EncryptHistoryMiddleware>();
        services.AddHttpClient<ISsrGateway, HttpSsrGateway>();

        return services;
    }

    /// <summary>
    /// Registers a shared props provider that runs on every Inertia request.
    /// Multiple providers are supported and merged in registration order.
    /// </summary>
    public static IServiceCollection AddInertiaSharedProps<TProvider>(this IServiceCollection services)
        where TProvider : class, Contracts.ISharedPropsProvider
    {
        services.AddScoped<Contracts.ISharedPropsProvider, TProvider>();
        return services;
    }

    /// <summary>
    /// Adds a health check for the Inertia SSR sidecar.
    /// </summary>
    public static IHealthChecksBuilder AddInertiaSsrCheck(
        this IHealthChecksBuilder builder,
        string name = "inertia-ssr",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddCheck<InertiaSsrHealthCheck>(
            name,
            failureStatus ?? HealthStatus.Degraded,
            tags ?? ["inertia", "ssr"]);
    }

    /// <summary>
    /// Enables the static Inertia helper for use without DI injection.
    /// DI injection of IInertiaResponseFactory is preferred for testability.
    /// </summary>
    public static IServiceCollection AddInertiaStaticHelper(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // Use a hosted service to initialize the static helper after DI is built
        services.AddSingleton<StaticHelperInitializer>();
        services.AddHostedService(sp => sp.GetRequiredService<StaticHelperInitializer>());

        return services;
    }

    private sealed class StaticHelperInitializer(IHttpContextAccessor accessor)
        : Microsoft.Extensions.Hosting.IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Inertia.Initialize(accessor);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
