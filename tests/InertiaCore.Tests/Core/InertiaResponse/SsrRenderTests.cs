using InertiaCore.Core;
using InertiaCore.Ssr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace InertiaCore.Tests.Core.InertiaResponse;

[Trait("Method", "ExecuteAsync")]
public class SsrRenderTests : InertiaResponseTestBase
{
    [Fact]
    public async Task Renders_ssr_body_via_gateway()
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(new SsrResponse("<title>SSR</title>", "<div id=\"app\"><h1>SSR</h1></div>"));

        var ctx = new InertiaResponseContext("App", null, SsrGateway: gateway);
        var response = new InertiaCore.Core.InertiaResponse("Test", new(), new(), ctx);
        var httpContext = CreateInertiaHttpContext();

        await response.ExecuteAsync(httpContext);

        var page = await ReadJsonResponse(httpContext);
        Assert.Equal("Test", page["component"].GetString());
    }

    [Fact]
    public async Task Gateway_exception_falls_back_to_csr()
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns<SsrResponse?>(_ => throw new Exception("unexpected"));

        var ctx = new InertiaResponseContext("App", null, SsrGateway: gateway);
        var response = new InertiaCore.Core.InertiaResponse("Test", new(), new(), ctx);
        var httpContext = CreateInertiaHttpContext();

        await response.ExecuteAsync(httpContext);

        var page = await ReadJsonResponse(httpContext);
        Assert.Equal("Test", page["component"].GetString());
    }

    [Fact]
    public async Task Gateway_exception_logs_when_logger_present()
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns<SsrResponse?>(_ => throw new InvalidOperationException("boom"));

        var logger = NullLogger.Instance;
        var ctx = new InertiaResponseContext("App", null, SsrGateway: gateway, Logger: logger);
        var response = new InertiaCore.Core.InertiaResponse("Test", new(), new(), ctx);
        var httpContext = CreateInertiaHttpContext();

        await response.ExecuteAsync(httpContext);

        // No exception thrown — graceful fallback
        var page = await ReadJsonResponse(httpContext);
        Assert.Equal("Test", page["component"].GetString());
    }

    [Fact]
    public async Task Response_works_with_all_history_flags()
    {
        var ctx = new InertiaResponseContext("App", "v1",
            EncryptHistory: true, ClearHistory: true, PreserveFragment: true);
        var response = new InertiaCore.Core.InertiaResponse("Test", new(), new(), ctx);
        var httpContext = CreateInertiaHttpContext();

        await response.ExecuteAsync(httpContext);

        var page = await ReadJsonResponse(httpContext);
        Assert.True(page["encryptHistory"].GetBoolean());
        Assert.True(page["clearHistory"].GetBoolean());
        Assert.True(page["preserveFragment"].GetBoolean());
    }
}
