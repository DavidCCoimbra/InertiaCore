using System.Text;
using InertiaCore.Constants;
using InertiaCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InertiaCore.Tests.Middleware;

[Trait("Class", "StreamingSsrMiddleware")]
public class StreamingSsrMiddlewareTests
{
    private const string InertiaHtml = """
        <!DOCTYPE html>
        <html><head><title>Test</title><link rel="stylesheet" href="app.css"></head>
        <body>
        <script data-page="app" type="application/json">{"component":"Users/Index","props":{"name":"Alice"},"url":"/users","version":"1.0.0"}</script>
        <div id="app"><div class="rendered-content">Server rendered HTML</div></div>
        </body></html>
        """;

    // -- Passthrough behavior --

    [Fact]
    public async Task Passes_through_inertia_xhr_requests()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Passes_through_non_html_responses()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.Body.Write(Encoding.UTF8.GetBytes("{\"ok\":true}"));
            return Task.CompletedTask;
        });
        var context = CreateContextWithCapture(out var responseBody);

        await middleware.InvokeAsync(context);

        var response = Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.Contains("ok", response);
    }

    [Fact]
    public async Task Passes_through_non_inertia_html()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "text/html";
            ctx.Response.Body.Write(Encoding.UTF8.GetBytes("<html><body>Regular page</body></html>"));
            return Task.CompletedTask;
        });
        var context = CreateContextWithCapture(out var responseBody);

        await middleware.InvokeAsync(context);

        var response = Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.Contains("Regular page", response);
        // Non-inertia HTML should NOT get chunked encoding
        Assert.DoesNotContain("chunked", context.Response.Headers["Transfer-Encoding"].ToString());
    }

    // -- Streaming behavior --

    [Fact]
    public async Task Sets_chunked_transfer_encoding_for_inertia_html()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "text/html";
            ctx.Response.Body.Write(Encoding.UTF8.GetBytes(InertiaHtml));
            return Task.CompletedTask;
        });
        var context = CreateContextWithCapture(out _);

        await middleware.InvokeAsync(context);

        Assert.Equal("chunked", context.Response.Headers["Transfer-Encoding"].ToString());
        Assert.Equal("no", context.Response.Headers["X-Accel-Buffering"].ToString());
        Assert.Equal("no-cache", context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Output_contains_shell_with_head_and_app_div()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "text/html";
            ctx.Response.Body.Write(Encoding.UTF8.GetBytes(InertiaHtml));
            return Task.CompletedTask;
        });
        var context = CreateContextWithCapture(out var responseBody);

        await middleware.InvokeAsync(context);

        var response = Encoding.UTF8.GetString(responseBody.ToArray());
        // Shell phase should contain head elements
        Assert.Contains("<title>Test</title>", response);
        Assert.Contains("app.css", response);
        Assert.Contains("<div id=\"app\">", response);
    }

    [Fact]
    public async Task Output_contains_ssr_content()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "text/html";
            ctx.Response.Body.Write(Encoding.UTF8.GetBytes(InertiaHtml));
            return Task.CompletedTask;
        });
        var context = CreateContextWithCapture(out var responseBody);

        await middleware.InvokeAsync(context);

        var response = Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.Contains("rendered-content", response);
        Assert.Contains("Server rendered HTML", response);
    }

    [Fact]
    public async Task Output_contains_hydration_data()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "text/html";
            ctx.Response.Body.Write(Encoding.UTF8.GetBytes(InertiaHtml));
            return Task.CompletedTask;
        });
        var context = CreateContextWithCapture(out var responseBody);

        await middleware.InvokeAsync(context);

        var response = Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.Contains("data-page", response);
        Assert.Contains("Users/Index", response);
    }

    [Fact]
    public async Task Head_appears_before_app_div_in_output()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "text/html";
            ctx.Response.Body.Write(Encoding.UTF8.GetBytes(InertiaHtml));
            return Task.CompletedTask;
        });
        var context = CreateContextWithCapture(out var responseBody);

        await middleware.InvokeAsync(context);

        var response = Encoding.UTF8.GetString(responseBody.ToArray());
        var headIdx = response.IndexOf("<title>Test</title>", StringComparison.Ordinal);
        var appDivIdx = response.IndexOf("<div id=\"app\">", StringComparison.Ordinal);
        var contentIdx = response.IndexOf("rendered-content", StringComparison.Ordinal);

        Assert.True(headIdx >= 0, "Head not found");
        Assert.True(appDivIdx >= 0, "App div not found");
        Assert.True(contentIdx >= 0, "Content not found");
        Assert.True(headIdx < appDivIdx, "Head should appear before app div");
        Assert.True(appDivIdx < contentIdx, "App div should appear before rendered content");
    }

    [Fact]
    public async Task Removes_content_length_for_chunked_response()
    {
        var middleware = CreateMiddleware(ctx =>
        {
            ctx.Response.ContentType = "text/html";
            ctx.Response.Body.Write(Encoding.UTF8.GetBytes(InertiaHtml));
            return Task.CompletedTask;
        });
        var context = CreateContextWithCapture(out _);

        await middleware.InvokeAsync(context);

        Assert.Null(context.Response.ContentLength);
    }

    // -- Helpers --

    private static StreamingSsrMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, Substitute.For<ILogger<StreamingSsrMiddleware>>());

    private static DefaultHttpContext CreateContextWithCapture(out MemoryStream responseBody)
    {
        var body = new MemoryStream();
        responseBody = body;
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        return context;
    }
}
