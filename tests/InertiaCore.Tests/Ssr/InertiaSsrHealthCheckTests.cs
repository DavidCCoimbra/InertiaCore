using InertiaCore.Ssr;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace InertiaCore.Tests.Ssr;

[Trait("Class", "InertiaSsrHealthCheck")]
public class InertiaSsrHealthCheckTests
{
    [Fact]
    public async Task Returns_healthy_when_ssr_is_reachable()
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);
        var healthCheck = new InertiaSsrHealthCheck(gateway);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Returns_degraded_when_ssr_is_unreachable()
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);
        var healthCheck = new InertiaSsrHealthCheck(gateway);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public async Task Passes_cancellation_token_to_gateway()
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);
        var healthCheck = new InertiaSsrHealthCheck(gateway);
        using var cts = new CancellationTokenSource();

        await healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        await gateway.Received(1).IsHealthyAsync(cts.Token);
    }
}
