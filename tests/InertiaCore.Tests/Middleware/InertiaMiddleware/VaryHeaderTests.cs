using InertiaCore.Constants;

namespace InertiaCore.Tests.Middleware.InertiaMiddleware;

[Trait("Method", "InvokeAsync")]
public class VaryHeaderTests : InertiaMiddlewareTestBase
{
    [Fact]
    public async Task Sets_vary_header_on_inertia_requests()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(isInertia: true);

        await middleware.InvokeAsync(context, NextOk);

        Assert.Contains(InertiaHeaders.Inertia, context.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task Sets_vary_header_on_non_inertia_requests()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(isInertia: false);

        await middleware.InvokeAsync(context, NextOk);

        Assert.Contains(InertiaHeaders.Inertia, context.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task Non_inertia_requests_pass_through_unmodified()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(isInertia: false);

        await middleware.InvokeAsync(context, NextOk);

        Assert.Equal(200, context.Response.StatusCode);
    }
}
