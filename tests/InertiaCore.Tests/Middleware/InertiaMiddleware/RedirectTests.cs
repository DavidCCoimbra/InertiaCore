using Microsoft.AspNetCore.Http;

namespace InertiaCore.Tests.Middleware.InertiaMiddleware;

[Trait("Method", "InvokeAsync")]
public class RedirectTests : InertiaMiddlewareTestBase
{
    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Converts_302_to_303_for_inertia_put_patch_delete(string method)
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(method: method, isInertia: true);

        await middleware.InvokeAsync(context, NextRedirect);

        Assert.Equal(StatusCodes.Status303SeeOther, context.Response.StatusCode);
    }

    [Fact]
    public async Task Does_not_convert_302_for_inertia_get()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(method: "GET", isInertia: true);

        await middleware.InvokeAsync(context, NextRedirect);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
    }

    [Fact]
    public async Task Does_not_convert_302_for_non_inertia_requests()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(method: "PUT", isInertia: false);

        await middleware.InvokeAsync(context, NextRedirect);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
    }

    [Fact]
    public async Task Does_not_convert_non_302_status_codes()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(method: "PUT", isInertia: true);

        await middleware.InvokeAsync(context, NextReturning(StatusCodes.Status301MovedPermanently));

        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
    }
}
