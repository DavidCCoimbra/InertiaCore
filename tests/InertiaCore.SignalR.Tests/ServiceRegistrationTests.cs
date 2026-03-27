using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.SignalR.Tests;

[Trait("Class", "ServiceRegistration")]
public class ServiceRegistrationTests
{
    [Fact]
    public void AddInertiaSignalR_registers_broadcaster()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInertiaSignalR();
        var provider = services.BuildServiceProvider();

        var broadcaster = provider.GetRequiredService<IInertiaBroadcaster>();

        Assert.IsType<InertiaBroadcaster>(broadcaster);
    }

    [Fact]
    public void Broadcaster_is_singleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInertiaSignalR();
        var provider = services.BuildServiceProvider();

        var b1 = provider.GetRequiredService<IInertiaBroadcaster>();
        var b2 = provider.GetRequiredService<IInertiaBroadcaster>();

        Assert.Same(b1, b2);
    }
}
