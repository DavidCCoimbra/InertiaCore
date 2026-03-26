using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Middleware;

/// <summary>
/// Middleware implementing the core Inertia HTTP protocol: version conflict detection,
/// redirect conversion, flash/error persistence, fragment handling, and Vary headers.
/// </summary>
public sealed class InertiaMiddleware : IMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var isInertia = context.Request.Headers.ContainsKey(InertiaHeaders.Inertia);

        if (isInertia && HasVersionMismatch(context))
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.Headers[InertiaHeaders.Location] = context.Request.GetEncodedUrl();
            return;
        }

        context.Response.Headers.Append("Vary", InertiaHeaders.Inertia);
        ShareDefaultProps(context);

        await next(context);

        HandlePostResponse(context, isInertia);
    }

    private static void ShareDefaultProps(HttpContext context)
    {
        var errorService = context.RequestServices.GetService<IInertiaErrorService>();
        var factory = context.RequestServices.GetRequiredService<IInertiaResponseFactory>();
        errorService?.ShareErrors(factory);
    }

    private static void HandlePostResponse(HttpContext context, bool isInertia)
    {
        if (IsRedirect(context))
        {
            PersistAndReflash(context);
        }

        if (isInertia && ShouldConvertRedirect(context))
        {
            context.Response.StatusCode = StatusCodes.Status303SeeOther;
        }

        if (isInertia && IsRedirect(context) && HasFragmentRedirect(context))
        {
            ConvertToFragmentRedirect(context);
        }
    }

    private static void PersistAndReflash(HttpContext context)
    {
        var flashService = context.RequestServices.GetService<IInertiaFlashService>();
        var errorService = context.RequestServices.GetService<IInertiaErrorService>();

        flashService?.Persist();
        flashService?.Reflash();
        errorService?.Reflash();
    }

    private static bool HasVersionMismatch(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var factory = context.RequestServices.GetRequiredService<IInertiaResponseFactory>();
        var clientVersion = context.Request.Headers[InertiaHeaders.Version].FirstOrDefault() ?? "";
        var serverVersion = factory.GetVersion() ?? "";

        return clientVersion != serverVersion;
    }

    private static bool ShouldConvertRedirect(HttpContext context)
    {
        return context.Response.StatusCode == StatusCodes.Status302Found
            && IsWriteMethod(context.Request.Method);
    }

    private static bool IsWriteMethod(string method)
    {
        return HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method)
            || HttpMethods.IsDelete(method);
    }

    private static bool IsRedirect(HttpContext context)
    {
        return context.Response.StatusCode is >= 300 and < 400;
    }

    private static bool HasFragmentRedirect(HttpContext context)
    {
        var location = context.Response.Headers.Location.FirstOrDefault();
        return location != null && location.Contains('#');
    }

    private static void ConvertToFragmentRedirect(HttpContext context)
    {
        var location = context.Response.Headers.Location.FirstOrDefault()!;
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.Headers.Remove("Location");
        context.Response.Headers[InertiaHeaders.Redirect] = location;
    }
}
