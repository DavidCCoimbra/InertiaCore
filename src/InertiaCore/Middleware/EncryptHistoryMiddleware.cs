using InertiaCore.Core;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Middleware;

/// <summary>
/// Middleware that enables history encryption for all Inertia responses.
/// Apply to specific routes or globally via the middleware pipeline.
/// </summary>
public class EncryptHistoryMiddleware(InertiaResponseFactory factory) : IMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        factory.EncryptHistory();

        await next(context);
    }
}

