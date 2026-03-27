using InertiaCore.Ssr;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.EmbeddedV8;

/// <summary>
/// Extension methods for registering the embedded V8 SSR engine.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the embedded V8 SSR engine. Replaces the default HTTP/MessagePack gateway
    /// with in-process V8 rendering — zero serialization, zero network.
    /// </summary>
    public static IServiceCollection AddInertiaEmbeddedV8(
        this IServiceCollection services,
        Action<EmbeddedSsrOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddSingleton<V8EnginePool>();
        services.AddSingleton<ISsrGateway, EmbeddedV8SsrGateway>();

        return services;
    }
}
