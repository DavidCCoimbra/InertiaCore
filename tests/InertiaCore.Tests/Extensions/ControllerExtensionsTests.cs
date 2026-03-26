using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Core;
using InertiaCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InertiaCore.Tests.Extensions;

[Trait("Class", "ControllerExtensions")]
public class ControllerExtensionsTests
{
    [Fact]
    public void RedirectBackWithErrors_stores_errors_in_tempdata()
    {
        var (controller, tempData) = CreateControllerWithModelErrors(
            ("Name", "Name is required"),
            ("Email", "Email is invalid"));

        controller.RedirectBackWithErrors();

        Assert.True(tempData.ContainsKey(SessionKeys.Errors));
        var json = tempData[SessionKeys.Errors] as string;
        var errors = JsonSerializer.Deserialize<Dictionary<string, string>>(json!);
        Assert.Equal("Name is required", errors!["Name"]);
        Assert.Equal("Email is invalid", errors["Email"]);
    }

    [Fact]
    public void RedirectBackWithErrors_redirects_to_referer()
    {
        var (controller, _) = CreateControllerWithModelErrors(("Name", "Required"));
        controller.Request.Headers.Referer = "http://localhost/form";

        var result = controller.RedirectBackWithErrors();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("http://localhost/form", redirect.Url);
    }

    [Fact]
    public void RedirectBackWithErrors_defaults_to_root_without_referer()
    {
        var (controller, _) = CreateControllerWithModelErrors(("Name", "Required"));

        var result = controller.RedirectBackWithErrors();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }

    [Fact]
    public void RedirectWithErrors_redirects_to_specified_url()
    {
        var (controller, _) = CreateControllerWithModelErrors(("Name", "Required"));

        var result = controller.RedirectWithErrors("/users");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/users", redirect.Url);
    }

    [Fact]
    public void Does_not_store_errors_when_modelstate_valid()
    {
        var (controller, tempData) = CreateController();

        controller.RedirectBackWithErrors();

        Assert.False(tempData.ContainsKey(SessionKeys.Errors));
    }

    [Fact]
    public void Error_bag_wraps_errors_under_bag_name()
    {
        var (controller, tempData) = CreateControllerWithModelErrors(("Name", "Required"));
        controller.Request.Headers[InertiaHeaders.ErrorBag] = "createUser";

        controller.RedirectBackWithErrors();

        var json = tempData[SessionKeys.Errors] as string;
        var bag = JsonSerializer.Deserialize<Dictionary<string, object?>>(json!);
        Assert.True(bag!.ContainsKey("createUser"));
    }

    private static (TestController Controller, ITempDataDictionary TempData) CreateControllerWithModelErrors(
        params (string Key, string Error)[] errors)
    {
        var (controller, tempData) = CreateController();

        foreach (var (key, error) in errors)
        {
            controller.ModelState.AddModelError(key, error);
        }

        return (controller, tempData);
    }

    private static (TestController Controller, ITempDataDictionary TempData) CreateController()
    {
        var tempData = new TestTempDataDictionary();
        var httpContext = new DefaultHttpContext();

        // Register IInertiaErrorService backed by real TempData
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(httpContext).Returns(tempData);
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(httpContextAccessor)
            .AddSingleton(tempDataFactory)
            .AddScoped<IInertiaErrorService, InertiaErrorService>()
            .BuildServiceProvider();

        var controller = new TestController
        {
            TempData = tempData,
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            },
        };

        return (controller, tempData);
    }

    public class TestController : Controller;

    private sealed class TestTempDataDictionary : Dictionary<string, object?>, ITempDataDictionary
    {
        public void Keep() { }
        public void Keep(string key) { }
        public void Load() { }
        public object? Peek(string key) => TryGetValue(key, out var v) ? v : null;
        public void Save() { }
    }
}
