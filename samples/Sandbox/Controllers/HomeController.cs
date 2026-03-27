using InertiaCore;
using InertiaCore.Attributes;
using InertiaCore.Core;
using Microsoft.AspNetCore.Mvc;

namespace Sandbox.Controllers;

public sealed class HomeController(IInertiaResponseFactory inertia) : Controller
{
    [HttpGet("/")]
    [InertiaInlinePageData]
    public IActionResult Index()
    {
        return inertia.Render("Home", new
        {
            Title = "Sandbox",
            Message = "Hello from InertiaCore!",
        });
    }

    [HttpGet("/props")]
    public IActionResult Props()
    {
        return inertia.Render("Props", new
        {
            Always = Inertia.Always("always-value"),
            Deferred = Inertia.Defer(() => "deferred-value"),
            Merged = Inertia.Merge(new[] { "a", "b", "c" }),
        });
    }
}
