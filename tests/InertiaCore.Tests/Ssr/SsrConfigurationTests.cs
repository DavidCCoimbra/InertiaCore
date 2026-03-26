using InertiaCore.Configuration;
using InertiaCore.Extensions;
using InertiaCore.Ssr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InertiaCore.Tests.Ssr;

[Trait("Class", "SsrConfiguration")]
public class SsrConfigurationTests
{
    [Fact]
    public void SsrOptions_defaults()
    {
        var options = new SsrOptions();

        Assert.False(options.Enabled);
        Assert.Equal("http://127.0.0.1:13714", options.Url);
        Assert.False(options.ThrowOnError);
        Assert.Equal(5, options.TimeoutSeconds);
        Assert.Empty(options.ExcludedPaths);
    }

    [Fact]
    public void SsrOptions_configurable_via_AddInertia()
    {
        var services = new ServiceCollection();
        services.AddInertia(o =>
        {
            o.Ssr.Enabled = true;
            o.Ssr.Url = "http://localhost:3000";
            o.Ssr.ThrowOnError = true;
            o.Ssr.TimeoutSeconds = 10;
            o.Ssr.ExcludedPaths = ["/admin", "/api"];
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<InertiaOptions>>().Value;

        Assert.True(options.Ssr.Enabled);
        Assert.Equal("http://localhost:3000", options.Ssr.Url);
        Assert.True(options.Ssr.ThrowOnError);
        Assert.Equal(10, options.Ssr.TimeoutSeconds);
        Assert.Equal(new[] { "/admin", "/api" }, options.Ssr.ExcludedPaths);
    }

    [Fact]
    public void ISsrGateway_registered_in_DI()
    {
        var services = new ServiceCollection();
        services.AddInertia();

        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISsrGateway));

        Assert.NotNull(descriptor);
    }

    [Fact]
    public void ISsrGateway_resolves_to_HttpSsrGateway()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInertia(o => o.Ssr.Enabled = true);
        var provider = services.BuildServiceProvider();

        var gateway = provider.GetRequiredService<ISsrGateway>();

        Assert.IsType<HttpSsrGateway>(gateway);
    }
}
