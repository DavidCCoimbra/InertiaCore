using InertiaCore.Constants;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Tests.Middleware.InertiaMiddleware;

[Trait("Method", "InvokeAsync")]
public class VersionCheckTests : InertiaMiddlewareTestBase
{
    [Fact]
    public async Task Matching_version_passes_through()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(
            isInertia: true,
            inertiaVersion: "v1",
            configureOptions: o => o.Version = "v1");

        await middleware.InvokeAsync(context, NextOk);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Mismatched_version_returns_409()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(
            isInertia: true,
            inertiaVersion: "old",
            configureOptions: o => o.Version = "new");

        await middleware.InvokeAsync(context, NextOk);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }

    [Fact]
    public async Task Mismatched_version_sets_location_header()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(
            path: "/dashboard",
            isInertia: true,
            inertiaVersion: "old",
            configureOptions: o => o.Version = "new");

        await middleware.InvokeAsync(context, NextOk);

        Assert.Contains("/dashboard", context.Response.Headers[InertiaHeaders.Location].ToString());
    }

    [Fact]
    public async Task Mismatched_version_does_not_call_next()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(
            isInertia: true,
            inertiaVersion: "old",
            configureOptions: o => o.Version = "new");

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Version_check_only_applies_to_get_requests()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext(
            method: "POST",
            isInertia: true,
            inertiaVersion: "old",
            configureOptions: o => o.Version = "new");

        await middleware.InvokeAsync(context, NextOk);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
