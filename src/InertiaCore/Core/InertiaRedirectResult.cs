using InertiaCore.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Core;

/// <summary>
/// A fluent redirect result for Inertia workflows. Supports chaining .WithErrors() and .WithFlash().
/// </summary>
public sealed class InertiaRedirectResult : IResult
{
    private readonly string _url;
    private Dictionary<string, string>? _errors;
    private string? _errorBag;
    private Dictionary<string, object?>? _flash;

    internal InertiaRedirectResult(string url)
    {
        _url = url;
    }

    /// <summary>
    /// Attaches validation errors to the redirect.
    /// </summary>
    public InertiaRedirectResult WithErrors(Dictionary<string, string> errors, string? errorBag = null)
    {
        _errors = errors;
        _errorBag = errorBag;
        return this;
    }

    /// <summary>
    /// Attaches flash data to the redirect.
    /// </summary>
    public InertiaRedirectResult WithFlash(string key, object? value)
    {
        _flash ??= new();
        _flash[key] = value;
        return this;
    }

    /// <summary>
    /// Attaches multiple flash data entries to the redirect.
    /// </summary>
    public InertiaRedirectResult WithFlash(Dictionary<string, object?> data)
    {
        _flash ??= new();
        foreach (var (key, value) in data)
        {
            _flash[key] = value;
        }

        return this;
    }

    /// <summary>
    /// Attaches arbitrary data to the redirect as flash. Alias for WithFlash matching Laravel's with().
    /// </summary>
    public InertiaRedirectResult With(string key, object? value) => WithFlash(key, value);

    /// <summary>
    /// Attaches multiple data entries to the redirect as flash. Alias for WithFlash matching Laravel's with().
    /// </summary>
    public InertiaRedirectResult With(Dictionary<string, object?> data) => WithFlash(data);

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
    {
        if (_errors is { Count: > 0 })
        {
            var errorBag = _errorBag
                ?? httpContext.Request.Headers[InertiaHeaders.ErrorBag].FirstOrDefault();

            var errorService = httpContext.RequestServices.GetRequiredService<IInertiaErrorService>();
            errorService.SetErrors(_errors, errorBag);
        }

        if (_flash is { Count: > 0 })
        {
            var factory = httpContext.RequestServices.GetRequiredService<IInertiaResponseFactory>();
            foreach (var (key, value) in _flash)
            {
                factory.Flash(key, value);
            }
        }

        return Results.Redirect(_url).ExecuteAsync(httpContext);
    }
}
