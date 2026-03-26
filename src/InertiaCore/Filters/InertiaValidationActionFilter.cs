using InertiaCore.Constants;
using InertiaCore.Core;
using InertiaCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Filters;

/// <summary>
/// MVC action filter that auto-redirects with validation errors on Inertia requests.
/// Non-Inertia requests pass through to ASP.NET Core's default behavior.
/// </summary>
public sealed class InertiaValidationActionFilter : IActionFilter
{
    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        if (!context.HttpContext.IsInertiaRequest())
        {
            return;
        }

        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.First().ErrorMessage);

        if (errors.Count == 0)
        {
            return;
        }

        var errorBag = context.HttpContext.Request.Headers[InertiaHeaders.ErrorBag].FirstOrDefault();
        var errorService = context.HttpContext.RequestServices.GetRequiredService<IInertiaErrorService>();
        errorService.SetErrors(errors, errorBag);

        var referer = context.HttpContext.Request.Headers.Referer.FirstOrDefault() ?? "/";
        context.Result = new RedirectResult(referer);
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
