using InertiaCore.Extensions;
using InertiaCore.Ssr;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Ssr;

[Trait("Class", "SsrTransportRegistration")]
public class SsrTransportRegistrationTests
{
    [Fact]
    public void Default_resolves_HttpSsrGateway()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        var gateway = provider.GetRequiredService<ISsrGateway>();

        Assert.IsType<HttpSsrGateway>(gateway);
    }

    [Fact]
    public void Http_gateway_is_scoped_via_HttpClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var gateway1 = scope1.ServiceProvider.GetRequiredService<ISsrGateway>();
        var gateway2 = scope2.ServiceProvider.GetRequiredService<ISsrGateway>();

        Assert.IsType<HttpSsrGateway>(gateway1);
        Assert.IsType<HttpSsrGateway>(gateway2);
    }
}
