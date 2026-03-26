using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Extensions;

/// <summary>
/// Extension methods for ASP.NET Core controllers to support Inertia.js workflows.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Redirects back to the referring page with ModelState validation errors stored in TempData.
    /// </summary>
    public static IActionResult RedirectBackWithErrors(this Controller controller)
    {
        StoreValidationErrors(controller);
        var referer = controller.Request.Headers.Referer.FirstOrDefault() ?? "/";
        return controller.Redirect(referer);
    }

    /// <summary>
    /// Redirects to the specified URL with ModelState validation errors stored in TempData.
    /// </summary>
    public static IActionResult RedirectWithErrors(this Controller controller, string url)
    {
        StoreValidationErrors(controller);
        return controller.Redirect(url);
    }

    private static void StoreValidationErrors(Controller controller)
    {
        var errors = controller.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.First().ErrorMessage);

        if (errors.Count == 0)
        {
            return;
        }

        var errorBag = controller.Request.Headers[InertiaHeaders.ErrorBag].FirstOrDefault();
        var errorService = controller.HttpContext.RequestServices.GetRequiredService<IInertiaErrorService>();
        errorService.SetErrors(errors, errorBag);
    }
}
