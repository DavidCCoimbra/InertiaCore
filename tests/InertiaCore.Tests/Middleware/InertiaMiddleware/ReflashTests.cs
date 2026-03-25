using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InertiaCore.Tests.Middleware.InertiaMiddleware;

[Trait("Method", "InvokeAsync")]
public class ReflashTests : InertiaMiddlewareTestBase
{
    [Fact]
    public async Task Redirect_calls_persist_and_reflash()
    {
        var middleware = CreateMiddleware();
        var flashService = Substitute.For<IInertiaFlashService>();
        var context = CreateHttpContextWithFlashService(flashService);

        await middleware.InvokeAsync(context, NextRedirect);

        flashService.Received(1).Persist();
        flashService.Received(1).Reflash();
    }

    [Fact]
    public async Task Non_redirect_does_not_persist_or_reflash()
    {
        var middleware = CreateMiddleware();
        var flashService = Substitute.For<IInertiaFlashService>();
        var context = CreateHttpContextWithFlashService(flashService);

        await middleware.InvokeAsync(context, NextOk);

        flashService.DidNotReceive().Persist();
        flashService.DidNotReceive().Reflash();
    }

    [Fact]
    public async Task Does_not_throw_without_flash_service()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, NextRedirect);
    }

    private static DefaultHttpContext CreateHttpContextWithFlashService(IInertiaFlashService flashService)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";

        var services = new ServiceCollection();
        services.AddSingleton(flashService);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
            new InertiaCore.Configuration.InertiaOptions()));
        services.AddScoped<InertiaResponseFactory>();
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }
}
