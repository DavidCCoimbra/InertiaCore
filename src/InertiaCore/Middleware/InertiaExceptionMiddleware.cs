using System.Text.Json;
using InertiaCore.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InertiaCore.Middleware;

/// <summary>
/// Developer exception middleware for Inertia requests. Renders errors as JSON for XHR
/// requests instead of breaking the SPA with a raw HTML error page.
/// Only active in Development environment.
/// </summary>
public sealed partial class InertiaExceptionMiddleware
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<InertiaExceptionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaExceptionMiddleware"/>.
    /// </summary>
    public InertiaExceptionMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        ILogger<InertiaExceptionMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware. Catches exceptions for Inertia requests in development.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_environment.IsDevelopment())
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            LogUnhandledException(_logger, ex);

            if (!IsInertiaRequest(context))
            {
                throw;
            }

            await WriteInertiaErrorResponse(context, ex);
        }
    }

    private static bool IsInertiaRequest(HttpContext context)
    {
        return context.Request.Headers.ContainsKey(InertiaHeaders.Inertia);
    }

    private static async Task WriteInertiaErrorResponse(HttpContext context, Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var error = new
        {
            component = "ErrorPage",
            props = new
            {
                status = 500,
                message = ex.Message,
                exception = ex.GetType().Name,
                stackTrace = ex.StackTrace,
            },
            url = $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}",
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, error, s_jsonOptions);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception during Inertia request")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}
