using System.Net;
using System.Text.Json;
using InertiaCore.Configuration;
using InertiaCore.Ssr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InertiaCore.Tests.Ssr;

[Trait("Class", "HttpSsrGateway")]
public class HttpSsrGatewayTests
{
    private static readonly ILogger<HttpSsrGateway> s_logger = NullLogger<HttpSsrGateway>.Instance;

    [Fact]
    public async Task RenderAsync_returns_null_when_disabled()
    {
        var gateway = CreateGateway(enabled: false);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_returns_ssr_response_on_success()
    {
        var responseData = new { head = new[] { "<title>Test</title>" }, body = "<div>Hello</div>" };
        var handler = new FakeHttpHandler(HttpStatusCode.OK,
            JsonSerializer.Serialize(responseData));
        var gateway = CreateGateway(handler: handler);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>
        {
            ["component"] = "Test",
        });

        Assert.NotNull(result);
        Assert.Equal("<title>Test</title>", result!.Head);
        Assert.Equal("<div>Hello</div>", result.Body);
    }

    [Fact]
    public async Task RenderAsync_joins_multiple_head_entries()
    {
        var responseData = new { head = new[] { "<title>A</title>", "<meta name=\"x\">" }, body = "<div/>" };
        var handler = new FakeHttpHandler(HttpStatusCode.OK,
            JsonSerializer.Serialize(responseData));
        var gateway = CreateGateway(handler: handler);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>());

        Assert.Contains("<title>A</title>", result!.Head);
        Assert.Contains("<meta name=\"x\">", result.Head);
    }

    [Fact]
    public async Task RenderAsync_returns_null_on_server_error()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "");
        var gateway = CreateGateway(handler: handler);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_throws_on_server_error_when_throw_enabled()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "");
        var gateway = CreateGateway(handler: handler, throwOnError: true);

        var ex = await Assert.ThrowsAsync<SsrException>(
            () => gateway.RenderAsync(new Dictionary<string, object?>()));

        Assert.Equal(SsrErrorType.RenderError, ex.ErrorType);
    }

    [Fact]
    public async Task RenderAsync_returns_null_on_connection_refused()
    {
        var handler = new FakeHttpHandler(exception: new HttpRequestException("Connection refused"));
        var gateway = CreateGateway(handler: handler);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_throws_on_connection_refused_when_throw_enabled()
    {
        var handler = new FakeHttpHandler(exception: new HttpRequestException("Connection refused"));
        var gateway = CreateGateway(handler: handler, throwOnError: true);

        var ex = await Assert.ThrowsAsync<SsrException>(
            () => gateway.RenderAsync(new Dictionary<string, object?>()));

        Assert.Equal(SsrErrorType.ConnectionRefused, ex.ErrorType);
    }

    [Fact]
    public async Task RenderAsync_returns_null_on_invalid_json()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "not-json");
        var gateway = CreateGateway(handler: handler);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_throws_on_invalid_json_when_throw_enabled()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "not-json");
        var gateway = CreateGateway(handler: handler, throwOnError: true);

        var ex = await Assert.ThrowsAsync<SsrException>(
            () => gateway.RenderAsync(new Dictionary<string, object?>()));

        Assert.Equal(SsrErrorType.InvalidResponse, ex.ErrorType);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task RenderAsync_returns_null_on_null_response_body()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "null");
        var gateway = CreateGateway(handler: handler);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_throws_on_null_response_when_throw_enabled()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "null");
        var gateway = CreateGateway(handler: handler, throwOnError: true);

        var ex = await Assert.ThrowsAsync<SsrException>(
            () => gateway.RenderAsync(new Dictionary<string, object?>()));

        Assert.Equal(SsrErrorType.InvalidResponse, ex.ErrorType);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public async Task RenderAsync_returns_null_on_cancellation()
    {
        var handler = new FakeHttpHandler(
            exception: new TaskCanceledException("cancelled"));
        var gateway = CreateGateway(handler: handler);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_returns_null_on_timeout()
    {
        var handler = new FakeHttpHandler(
            exception: new TaskCanceledException("timeout",
                new TimeoutException("timed out")));
        var gateway = CreateGateway(handler: handler);

        var result = await gateway.RenderAsync(new Dictionary<string, object?>());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderAsync_invokes_onRenderFailed_callback()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "");
        SsrRenderFailed? captured = null;
        var gateway = CreateGateway(handler: handler,
            onRenderFailed: e => captured = e);

        await gateway.RenderAsync(new Dictionary<string, object?> { ["test"] = true });

        Assert.NotNull(captured);
        Assert.Equal(SsrErrorType.RenderError, captured!.ErrorType);
        Assert.NotNull(captured.Page);
    }

    [Fact]
    public async Task IsHealthyAsync_returns_false_when_disabled()
    {
        var gateway = CreateGateway(enabled: false);

        Assert.False(await gateway.IsHealthyAsync());
    }

    [Fact]
    public async Task IsHealthyAsync_returns_true_on_200()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "ok");
        var gateway = CreateGateway(handler: handler);

        Assert.True(await gateway.IsHealthyAsync());
    }

    [Fact]
    public async Task IsHealthyAsync_returns_false_on_error()
    {
        var handler = new FakeHttpHandler(exception: new HttpRequestException("down"));
        var gateway = CreateGateway(handler: handler);

        Assert.False(await gateway.IsHealthyAsync());
    }

    private static HttpSsrGateway CreateGateway(
        FakeHttpHandler? handler = null,
        bool enabled = true,
        bool throwOnError = false,
        Action<SsrRenderFailed>? onRenderFailed = null)
    {
        var httpClient = new HttpClient(handler ?? new FakeHttpHandler(HttpStatusCode.OK, "{}"));
        var options = new InertiaOptions
        {
            Ssr = new SsrOptions
            {
                Enabled = enabled,
                Url = "http://localhost:13714",
                ThrowOnError = throwOnError,
                TimeoutSeconds = 5,
            },
        };

        return new HttpSsrGateway(httpClient, Options.Create(options), s_logger, onRenderFailed);
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;
        private readonly Exception? _exception;

        public FakeHttpHandler(HttpStatusCode statusCode = HttpStatusCode.OK, string content = "{}")
        {
            _statusCode = statusCode;
            _content = content;
        }

        public FakeHttpHandler(Exception exception)
        {
            _statusCode = HttpStatusCode.OK;
            _content = "";
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
