using InertiaCore.Configuration;
using InertiaCore.Core;
using InertiaCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Tests.Middleware;

[Trait("Class", "EncryptHistoryMiddleware")]
public class EncryptHistoryMiddlewareTests
{
    [Fact]
    public async Task Enables_encrypt_history_on_factory()
    {
        var flashService = Substitute.For<IInertiaFlashService>();
        var factory = new InertiaResponseFactory(
            Options.Create(new InertiaOptions()), flashService);
        var middleware = new EncryptHistoryMiddleware(factory);

        var nextCalled = false;
        await middleware.InvokeAsync(new DefaultHttpContext(), _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Render_after_middleware_includes_encryptHistory()
    {
        var flashService = Substitute.For<IInertiaFlashService>();
        var factory = new InertiaResponseFactory(
            Options.Create(new InertiaOptions()), flashService);
        var middleware = new EncryptHistoryMiddleware(factory);

        await middleware.InvokeAsync(new DefaultHttpContext(), _ => Task.CompletedTask);

        var response = factory.Render("Test");
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Inertia"] = "true";
        context.Response.Body = new MemoryStream();

        await response.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        var page = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Text.Json.JsonElement>(
            context.Response.Body);
        Assert.True(page.TryGetProperty("encryptHistory", out var val));
        Assert.True(val.GetBoolean());
    }
}
