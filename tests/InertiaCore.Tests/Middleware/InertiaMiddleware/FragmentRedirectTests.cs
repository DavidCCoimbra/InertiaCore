using InertiaCore.Constants;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Tests.Middleware.InertiaMiddleware;

[Trait("Method", "InvokeAsync")]
public class FragmentRedirectTests : InertiaMiddlewareTestBase
{
    [Fact]
    public async Task Fragment_redirect_converts_to_409_with_x_inertia_redirect()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(isInertia: true, inertiaVersion: "1.0.0",
            configureOptions: o => o.Version = "1.0.0");

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "/page#section";
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("/page#section", context.Response.Headers[InertiaHeaders.Redirect].ToString());
        Assert.Empty(context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Non_fragment_redirect_keeps_normal_status()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(isInertia: true, inertiaVersion: "1.0.0",
            configureOptions: o => o.Version = "1.0.0");

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "/page";
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/page", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Fragment_redirect_ignored_for_non_inertia_requests()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(isInertia: false);

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status302Found;
            ctx.Response.Headers.Location = "/page#section";
            return Task.CompletedTask;
        });

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
    }
}
