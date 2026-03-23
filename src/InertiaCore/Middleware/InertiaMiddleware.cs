using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Middleware;

/// <summary>
/// Middleware implementing the core Inertia HTTP protocol: version conflict detection,
/// redirect conversion, and Vary headers.
/// </summary>
public class InertiaMiddleware : IMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var isInertia = context.Request.Headers.ContainsKey(InertiaHeaders.Inertia);

        // Version check: Inertia GET with mismatched version → 409
        if (isInertia && HttpMethods.IsGet(context.Request.Method))
        {
            var factory = context.RequestServices.GetRequiredService<InertiaResponseFactory>();
            var clientVersion = context.Request.Headers[InertiaHeaders.Version].FirstOrDefault() ?? "";
            var serverVersion = factory.GetVersion() ?? "";

            if (clientVersion != serverVersion)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.Headers[InertiaHeaders.Location] = context.Request.GetEncodedUrl();
                return;
            }
        }

        // Vary header — set before next() since the response body may start streaming
        context.Response.Headers.Append("Vary", InertiaHeaders.Inertia);

        await next(context);

        // 302 → 303 for PUT/PATCH/DELETE — safe to modify after next() because
        // redirect responses don't write a body (headers are still mutable)
        if (isInertia
            && context.Response.StatusCode == StatusCodes.Status302Found
            && (HttpMethods.IsPut(context.Request.Method)
                || HttpMethods.IsPatch(context.Request.Method)
                || HttpMethods.IsDelete(context.Request.Method)))
        {
            context.Response.StatusCode = StatusCodes.Status303SeeOther;
        }
    }
}
