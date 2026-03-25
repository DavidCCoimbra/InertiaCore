using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Middleware;

/// <summary>
/// Middleware implementing the core Inertia HTTP protocol: version conflict detection,
/// redirect conversion, flash persistence, and Vary headers.
/// </summary>
public class InertiaMiddleware : IMiddleware
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

        await next(context);

        var flashService = context.RequestServices.GetService<IInertiaFlashService>();

        if (IsRedirect(context))
        {
            flashService?.Persist();
            flashService?.Reflash();
        }

        if (isInertia && ShouldConvertRedirect(context))
        {
            context.Response.StatusCode = StatusCodes.Status303SeeOther;
        }
    }

    private static bool HasVersionMismatch(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var factory = context.RequestServices.GetRequiredService<InertiaResponseFactory>();
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
}
