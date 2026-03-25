using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Class", "InertiaFlashService")]
public class FlashPersistenceTests
{
    [Fact]
    public void Flash_stores_value()
    {
        var (service, _) = CreateServiceWithTempData();

        service.Flash("key", "value");

        Assert.Equal("value", service.GetPending()["key"]);
    }

    [Fact]
    public void Flash_dictionary_stores_multiple()
    {
        var (service, _) = CreateServiceWithTempData();

        service.Flash(new Dictionary<string, object?> { ["a"] = "1", ["b"] = "2" });

        Assert.Equal(2, service.GetPending().Count);
    }

    [Fact]
    public void GetPending_returns_empty_by_default()
    {
        var (service, _) = CreateServiceWithTempData();

        Assert.Empty(service.GetPending());
    }

    [Fact]
    public void Persist_writes_to_tempdata()
    {
        var (service, tempData) = CreateServiceWithTempData();

        service.Flash("success", "Done!");
        service.Persist();

        Assert.True(tempData.ContainsKey(SessionKeys.FlashData));
        var json = tempData[SessionKeys.FlashData] as string;
        var flash = JsonSerializer.Deserialize<Dictionary<string, object?>>(json!);
        Assert.Equal("Done!", flash!["success"]?.ToString());
    }

    [Fact]
    public void Persist_batches_multiple_flash_values()
    {
        var (service, tempData) = CreateServiceWithTempData();

        service.Flash("a", "1");
        service.Flash("b", "2");
        service.Persist();

        var json = tempData[SessionKeys.FlashData] as string;
        var flash = JsonSerializer.Deserialize<Dictionary<string, object?>>(json!);
        Assert.Equal(2, flash!.Count);
    }

    [Fact]
    public void Persist_does_nothing_when_no_flash()
    {
        var (service, tempData) = CreateServiceWithTempData();

        service.Persist();

        Assert.False(tempData.ContainsKey(SessionKeys.FlashData));
    }

    [Fact]
    public void Persist_without_httpcontext_does_not_throw()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var service = new InertiaFlashService(httpContextAccessor);

        service.Flash("key", "value");
        service.Persist();
    }

    [Fact]
    public void Consume_returns_flash_from_tempdata()
    {
        var (service, tempData) = CreateServiceWithTempData();
        tempData[SessionKeys.FlashData] = JsonSerializer.Serialize(
            new Dictionary<string, object?> { ["msg"] = "hello" });

        var flash = service.Consume();

        Assert.NotNull(flash);
        Assert.Equal("hello", flash!["msg"]?.ToString());
    }

    [Fact]
    public void Consume_returns_null_when_no_flash()
    {
        var (service, _) = CreateServiceWithTempData();

        var flash = service.Consume();

        Assert.Null(flash);
    }

    [Fact]
    public void Consume_without_httpcontext_returns_null()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var service = new InertiaFlashService(httpContextAccessor);

        Assert.Null(service.Consume());
    }

    [Fact]
    public void Reflash_keeps_flash_alive()
    {
        var (service, tempData) = CreateServiceWithTempData();
        tempData[SessionKeys.FlashData] = "{}";

        service.Reflash();

        // Flash data should still be present (Keep prevents consumption)
        Assert.True(tempData.ContainsKey(SessionKeys.FlashData));
    }

    [Fact]
    public void Reflash_does_nothing_when_no_flash()
    {
        var (service, tempData) = CreateServiceWithTempData();

        service.Reflash();

        Assert.False(tempData.ContainsKey(SessionKeys.FlashData));
    }

    [Fact]
    public void Reflash_without_httpcontext_does_not_throw()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var service = new InertiaFlashService(httpContextAccessor);

        service.Reflash();
    }

    private static (InertiaFlashService Service, ITempDataDictionary TempData) CreateServiceWithTempData()
    {
        var tempData = new TestTempDataDictionary();
        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();

        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddSingleton(tempDataFactory);
        httpContext.RequestServices = services.BuildServiceProvider();

        tempDataFactory.GetTempData(Arg.Any<HttpContext>()).Returns(tempData);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        return (new InertiaFlashService(httpContextAccessor), tempData);
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
