using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Middleware;

/// <summary>
/// Middleware that enables history encryption for all Inertia responses.
/// Apply to specific routes or globally via the middleware pipeline.
/// </summary>
public class EncryptHistoryMiddleware : IMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var factory = context.RequestServices.GetRequiredService<InertiaResponseFactory>();
        factory.EncryptHistory();

        await next(context);
    }
}
