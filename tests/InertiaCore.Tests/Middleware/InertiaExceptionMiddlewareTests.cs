using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace InertiaCore.Tests.Middleware;

[Trait("Class", "InertiaExceptionMiddleware")]
public class InertiaExceptionMiddlewareTests
{
    [Fact]
    public async Task Returns_json_error_for_inertia_request_in_development()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Development");

        var middleware = new InertiaExceptionMiddleware(
            _ => throw new InvalidOperationException("Something broke"),
            env,
            NullLogger<InertiaExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        Assert.Equal("ErrorPage", json.GetProperty("component").GetString());
        Assert.Equal("Something broke", json.GetProperty("props").GetProperty("message").GetString());
        Assert.Equal("InvalidOperationException", json.GetProperty("props").GetProperty("exception").GetString());
    }

    [Fact]
    public async Task Rethrows_for_non_inertia_request()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Development");

        var middleware = new InertiaExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            env,
            NullLogger<InertiaExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task Passes_through_in_production()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Production");

        var nextCalled = false;
        var middleware = new InertiaExceptionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            env,
            NullLogger<InertiaExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Does_not_catch_in_production()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Production");

        var middleware = new InertiaExceptionMiddleware(
            _ => throw new InvalidOperationException("prod error"),
            env,
            NullLogger<InertiaExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task Includes_stack_trace_in_response()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Development");

        var middleware = new InertiaExceptionMiddleware(
            _ => throw new InvalidOperationException("trace test"),
            env,
            NullLogger<InertiaExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        var stackTrace = json.GetProperty("props").GetProperty("stackTrace").GetString();
        Assert.NotNull(stackTrace);
        Assert.Contains("InertiaExceptionMiddlewareTests", stackTrace!);
    }
}
