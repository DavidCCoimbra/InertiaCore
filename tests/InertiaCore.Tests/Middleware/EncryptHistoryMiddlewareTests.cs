using InertiaCore.Configuration;
using InertiaCore.Core;
using InertiaCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Tests.Middleware;

[Trait("Class", "EncryptHistoryMiddleware")]
public class EncryptHistoryMiddlewareTests
{
    [Fact]
    public async Task Calls_next()
    {
        var middleware = new EncryptHistoryMiddleware();
        var context = CreateHttpContext();

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Render_after_middleware_includes_encryptHistory()
    {
        var middleware = new EncryptHistoryMiddleware();
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        var factory = context.RequestServices.GetRequiredService<InertiaResponseFactory>();
        var response = factory.Render("Test");
        var renderContext = new DefaultHttpContext();
        renderContext.Request.Headers["X-Inertia"] = "true";
        renderContext.Response.Body = new MemoryStream();

        await response.ExecuteAsync(renderContext);

        renderContext.Response.Body.Position = 0;
        var page = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Text.Json.JsonElement>(
            renderContext.Response.Body);
        Assert.True(page.TryGetProperty("encryptHistory", out var val));
        Assert.True(val.GetBoolean());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IInertiaFlashService>());
        services.AddSingleton(Options.Create(new InertiaOptions()));
        services.AddScoped<InertiaResponseFactory>();
        context.RequestServices = services.BuildServiceProvider();
        return context;
    }
}
