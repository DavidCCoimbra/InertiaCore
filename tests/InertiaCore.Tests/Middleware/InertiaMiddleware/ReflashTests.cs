using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InertiaCore.Tests.Middleware.InertiaMiddleware;

[Trait("Method", "InvokeAsync")]
public class ReflashTests : InertiaMiddlewareTestBase
{
    [Fact]
    public async Task Redirect_calls_flash_persist_and_reflash()
    {
        var middleware = CreateMiddleware();
        var flashService = Substitute.For<IInertiaFlashService>();
        var errorService = Substitute.For<IInertiaErrorService>();
        var context = CreateHttpContextWithServices(flashService, errorService);

        await middleware.InvokeAsync(context, NextRedirect);

        flashService.Received(1).Persist();
        flashService.Received(1).Reflash();
    }

    [Fact]
    public async Task Redirect_calls_error_reflash()
    {
        var middleware = CreateMiddleware();
        var flashService = Substitute.For<IInertiaFlashService>();
        var errorService = Substitute.For<IInertiaErrorService>();
        var context = CreateHttpContextWithServices(flashService, errorService);

        await middleware.InvokeAsync(context, NextRedirect);

        errorService.Received(1).Reflash();
    }

    [Fact]
    public async Task Non_redirect_does_not_persist_or_reflash()
    {
        var middleware = CreateMiddleware();
        var flashService = Substitute.For<IInertiaFlashService>();
        var errorService = Substitute.For<IInertiaErrorService>();
        var context = CreateHttpContextWithServices(flashService, errorService);

        await middleware.InvokeAsync(context, NextOk);

        flashService.DidNotReceive().Persist();
        flashService.DidNotReceive().Reflash();
        errorService.DidNotReceive().Reflash();
    }

    [Fact]
    public async Task ShareErrors_called_before_next()
    {
        var middleware = CreateMiddleware();
        var errorService = Substitute.For<IInertiaErrorService>();
        var context = CreateHttpContextWithServices(Substitute.For<IInertiaFlashService>(), errorService);

        await middleware.InvokeAsync(context, NextOk);

        errorService.Received(1).ShareErrors(Arg.Any<InertiaResponseFactory>());
    }

    [Fact]
    public async Task Does_not_throw_without_services()
    {
        var middleware = CreateMiddleware();
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context, NextRedirect);
    }

    private static DefaultHttpContext CreateHttpContextWithServices(
        IInertiaFlashService flashService,
        IInertiaErrorService errorService)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";

        var services = new ServiceCollection();
        services.AddSingleton(flashService);
        services.AddSingleton(errorService);
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
            new InertiaCore.Configuration.InertiaOptions()));
        services.AddScoped<IInertiaResponseFactory, InertiaResponseFactory>();
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }
}
