using InertiaCore.Configuration;
using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InertiaCore.Tests.Middleware.InertiaMiddleware;

[Trait("Class", "InertiaMiddleware")]
public abstract class InertiaMiddlewareTestBase
{
    protected static InertiaCore.Middleware.InertiaMiddleware CreateMiddleware() => new();

    protected static DefaultHttpContext CreateHttpContext(
        string method = "GET",
        string path = "/",
        bool isInertia = false,
        string? inertiaVersion = null,
        Action<InertiaOptions>? configureOptions = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;

        if (isInertia)
        {
            context.Request.Headers[InertiaHeaders.Inertia] = "true";
        }

        if (inertiaVersion != null)
        {
            context.Request.Headers[InertiaHeaders.Version] = inertiaVersion;
        }

        var services = new ServiceCollection();
        var options = new InertiaOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<IInertiaFlashService, InertiaFlashService>();
        services.AddScoped<IInertiaErrorService, InertiaErrorService>();
        services.AddScoped<InertiaResponseFactory>();
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }

    protected static RequestDelegate NextReturning(int statusCode) =>
        context =>
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };

    protected static RequestDelegate NextOk => NextReturning(StatusCodes.Status200OK);

    protected static RequestDelegate NextRedirect => NextReturning(StatusCodes.Status302Found);
}
