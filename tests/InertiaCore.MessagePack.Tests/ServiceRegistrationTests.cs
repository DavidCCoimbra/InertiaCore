using InertiaCore.Extensions;
using InertiaCore.Ssr;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.MessagePack.Tests;

[Trait("Class", "ServiceRegistration")]
public class ServiceRegistrationTests
{
    [Fact]
    public void AddInertiaMessagePack_overrides_gateway()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInertia();
        services.AddInertiaMessagePack();
        var provider = services.BuildServiceProvider();

        var gateway = provider.GetRequiredService<ISsrGateway>();

        Assert.IsType<MessagePackSsrGateway>(gateway);
    }

    [Fact]
    public void Without_MessagePack_resolves_HttpSsrGateway()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        var gateway = provider.GetRequiredService<ISsrGateway>();

        Assert.IsType<HttpSsrGateway>(gateway);
    }
}
