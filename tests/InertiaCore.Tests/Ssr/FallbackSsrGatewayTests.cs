using InertiaCore.Ssr;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InertiaCore.Tests.Ssr;

[Trait("Class", "FallbackSsrGateway")]
public class FallbackSsrGatewayTests
{
    [Fact]
    public async Task Uses_primary_when_it_succeeds()
    {
        var primary = Substitute.For<ISsrGateway>();
        var fallback = Substitute.For<ISsrGateway>();
        var expected = new SsrResponse("head", "body");
        primary.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var gateway = CreateGateway(primary, fallback);
        var result = await gateway.RenderAsync(CreatePage());

        Assert.Same(expected, result);
        await fallback.DidNotReceive().RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_back_when_primary_returns_null()
    {
        var primary = Substitute.For<ISsrGateway>();
        var fallback = Substitute.For<ISsrGateway>();
        var expected = new SsrResponse("head", "body");
        primary.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns((SsrResponse?)null);
        fallback.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var gateway = CreateGateway(primary, fallback);
        var result = await gateway.RenderAsync(CreatePage());

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Falls_back_when_primary_throws()
    {
        var primary = Substitute.For<ISsrGateway>();
        var fallback = Substitute.For<ISsrGateway>();
        var expected = new SsrResponse("head", "body");
        primary.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("V8 crashed"));
        fallback.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var gateway = CreateGateway(primary, fallback);
        var result = await gateway.RenderAsync(CreatePage());

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Returns_null_when_all_fail()
    {
        var primary = Substitute.For<ISsrGateway>();
        var fallback = Substitute.For<ISsrGateway>();
        primary.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns((SsrResponse?)null);
        fallback.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns((SsrResponse?)null);

        var gateway = CreateGateway(primary, fallback);
        var result = await gateway.RenderAsync(CreatePage());

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_null_on_cancellation()
    {
        var primary = Substitute.For<ISsrGateway>();
        primary.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var gateway = CreateGateway(primary);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await gateway.RenderAsync(CreatePage(), cts.Token);

        Assert.Null(result);
    }

    [Fact]
    public async Task IsHealthy_returns_true_if_any_gateway_healthy()
    {
        var primary = Substitute.For<ISsrGateway>();
        var fallback = Substitute.For<ISsrGateway>();
        primary.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);
        fallback.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var gateway = CreateGateway(primary, fallback);
        var result = await gateway.IsHealthyAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsHealthy_returns_false_if_all_unhealthy()
    {
        var primary = Substitute.For<ISsrGateway>();
        var fallback = Substitute.For<ISsrGateway>();
        primary.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);
        fallback.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);

        var gateway = CreateGateway(primary, fallback);
        var result = await gateway.IsHealthyAsync();

        Assert.False(result);
    }

    private static FallbackSsrGateway CreateGateway(params ISsrGateway[] gateways) =>
        new(gateways, Substitute.For<ILogger<FallbackSsrGateway>>());

    private static Dictionary<string, object?> CreatePage() => new()
    {
        ["component"] = "Test",
        ["props"] = new Dictionary<string, object?>(),
        ["url"] = "/",
        ["version"] = "1.0.0",
    };
}
