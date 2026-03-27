using InertiaCore.Ssr;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.MessagePack;

/// <summary>
/// Extension methods for registering the MessagePack SSR gateway.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the MessagePack SSR gateway. Replaces the default HTTP gateway with
    /// binary MessagePack serialization over Unix Domain Sockets (~4x faster IPC).
    /// </summary>
    public static IServiceCollection AddInertiaMessagePack(this IServiceCollection services)
    {
        services.AddSingleton<ISsrGateway, MessagePackSsrGateway>();
        return services;
    }
}
