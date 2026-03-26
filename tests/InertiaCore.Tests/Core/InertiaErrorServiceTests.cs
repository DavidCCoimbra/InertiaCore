using System.Text.Json;
using InertiaCore.Configuration;
using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Tests.Core;

[Trait("Class", "InertiaErrorService")]
public class InertiaErrorServiceTests
{
    [Fact]
    public async Task ShareErrors_adds_errors_to_props()
    {
        var errors = new Dictionary<string, string> { ["name"] = "Required" };
        var (service, factory) = CreateServiceWithErrors(errors);

        service.ShareErrors(factory);
        var response = factory.Render("Test");
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        var page = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        var propsErrors = page.GetProperty("props").GetProperty("errors");
        Assert.Equal("Required", propsErrors.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ShareErrors_returns_empty_when_no_errors()
    {
        var (service, factory) = CreateServiceWithErrors(null);

        service.ShareErrors(factory);
        var response = factory.Render("Test");
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        var page = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        var propsErrors = page.GetProperty("props").GetProperty("errors");
        Assert.Empty(propsErrors.EnumerateObject());
    }

    [Fact]
    public void Reflash_keeps_errors_in_tempdata()
    {
        var (service, tempData) = CreateServiceWithTempData();
        tempData[SessionKeys.Errors] = "{}";

        service.Reflash();

        Assert.True(tempData.ContainsKey(SessionKeys.Errors));
    }

    [Fact]
    public void Reflash_does_nothing_when_no_errors()
    {
        var (service, tempData) = CreateServiceWithTempData();

        service.Reflash();

        Assert.False(tempData.ContainsKey(SessionKeys.Errors));
    }

    [Fact]
    public void ShareErrors_without_httpcontext_does_not_throw()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var service = new InertiaErrorService(httpContextAccessor);
        var flashService = Substitute.For<IInertiaFlashService>();
        var factory = new InertiaResponseFactory(Options.Create(new InertiaOptions()), flashService, Substitute.For<IHttpContextAccessor>());

        service.ShareErrors(factory);
    }

    [Fact]
    public void Reflash_without_httpcontext_does_not_throw()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var service = new InertiaErrorService(httpContextAccessor);

        service.Reflash();
    }

    [Fact]
    public void ShareErrors_without_tempdata_provider_does_not_throw()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var service = new InertiaErrorService(httpContextAccessor);
        var flashService = Substitute.For<IInertiaFlashService>();
        var factory = new InertiaResponseFactory(Options.Create(new InertiaOptions()), flashService, Substitute.For<IHttpContextAccessor>());

        service.ShareErrors(factory);
    }

    [Fact]
    public async Task ConsumeErrors_returns_empty_when_no_tempdata_provider()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var service = new InertiaErrorService(httpContextAccessor);
        var flashService = Substitute.For<IInertiaFlashService>();
        var factory = new InertiaResponseFactory(Options.Create(new InertiaOptions()), flashService, Substitute.For<IHttpContextAccessor>());

        service.ShareErrors(factory);

        // Render to trigger lazy AlwaysProp resolution
        var response = factory.Render("Test");
        var ctx = CreateInertiaHttpContext();
        await response.ExecuteAsync(ctx);

        ctx.Response.Body.Position = 0;
        var page = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Text.Json.JsonElement>(ctx.Response.Body);
        var errors = page.GetProperty("props").GetProperty("errors");
        Assert.Empty(errors.EnumerateObject());
    }

    [Fact]
    public void Reflash_without_tempdata_provider_does_not_throw()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection().BuildServiceProvider();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var service = new InertiaErrorService(httpContextAccessor);

        service.Reflash();
    }

    private static (InertiaErrorService Service, InertiaResponseFactory Factory) CreateServiceWithErrors(
        Dictionary<string, string>? errors)
    {
        var tempData = new TestTempDataDictionary();
        if (errors != null)
        {
            tempData[SessionKeys.Errors] = JsonSerializer.Serialize(errors);
        }

        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(Arg.Any<HttpContext>()).Returns(tempData);

        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddSingleton(tempDataFactory);
        httpContext.RequestServices = services.BuildServiceProvider();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var errorService = new InertiaErrorService(httpContextAccessor);
        var flashService = Substitute.For<IInertiaFlashService>();
        var factory = new InertiaResponseFactory(Options.Create(new InertiaOptions()), flashService, Substitute.For<IHttpContextAccessor>());

        return (errorService, factory);
    }

    private static (InertiaErrorService Service, TestTempDataDictionary TempData) CreateServiceWithTempData()
    {
        var tempData = new TestTempDataDictionary();
        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(Arg.Any<HttpContext>()).Returns(tempData);

        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddSingleton(tempDataFactory);
        httpContext.RequestServices = services.BuildServiceProvider();

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return (new InertiaErrorService(httpContextAccessor), tempData);
    }

    private static DefaultHttpContext CreateInertiaHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";
        context.Response.Body = new MemoryStream();
        return context;
    }

    // -- SetErrors --

    [Fact]
    public void SetErrors_stores_errors_in_tempdata()
    {
        var (service, tempData) = CreateServiceWithTempData();

        service.SetErrors(new() { ["Name"] = "Required", ["Email"] = "Invalid" });

        Assert.True(tempData.ContainsKey(SessionKeys.Errors));
        var errors = JsonSerializer.Deserialize<Dictionary<string, string>>(tempData[SessionKeys.Errors] as string ?? "");
        Assert.Equal("Required", errors!["Name"]);
        Assert.Equal("Invalid", errors["Email"]);
    }

    [Fact]
    public void SetErrors_with_error_bag_wraps_under_bag_name()
    {
        var (service, tempData) = CreateServiceWithTempData();

        service.SetErrors(new() { ["Name"] = "Required" }, errorBag: "createUser");

        var json = tempData[SessionKeys.Errors] as string;
        var bag = JsonSerializer.Deserialize<Dictionary<string, object?>>(json!);
        Assert.True(bag!.ContainsKey("createUser"));
    }

    [Fact]
    public void SetErrors_does_nothing_when_empty()
    {
        var (service, tempData) = CreateServiceWithTempData();

        service.SetErrors(new());

        Assert.False(tempData.ContainsKey(SessionKeys.Errors));
    }

    [Fact]
    public void SetErrors_without_tempdata_does_not_throw()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var service = new InertiaErrorService(httpContextAccessor);

        service.SetErrors(new() { ["Name"] = "Required" });
    }

    private sealed class TestTempDataDictionary : Dictionary<string, object?>, ITempDataDictionary
    {
        public void Keep() { }
        public void Keep(string key) { }
        public void Load() { }
        public object? Peek(string key) => TryGetValue(key, out var v) ? v : null;
        public void Save() { }
    }
}
