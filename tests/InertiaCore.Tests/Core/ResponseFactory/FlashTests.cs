using InertiaCore.Configuration;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Method", "Flash")]
public class FlashTests
{
    [Fact]
    public void Flash_delegates_to_flash_service()
    {
        var flashService = Substitute.For<IInertiaFlashService>();
        var factory = new InertiaResponseFactory(Options.Create(new InertiaOptions()), flashService, Substitute.For<IHttpContextAccessor>());

        factory.Flash("success", "Done!");

        flashService.Received(1).Flash("success", "Done!");
    }

    [Fact]
    public void Flash_dictionary_delegates_to_flash_service()
    {
        var flashService = Substitute.For<IInertiaFlashService>();
        var factory = new InertiaResponseFactory(Options.Create(new InertiaOptions()), flashService, Substitute.For<IHttpContextAccessor>());
        var data = new Dictionary<string, object?> { ["a"] = "1", ["b"] = "2" };

        factory.Flash(data);

        flashService.Received(1).Flash(data);
    }

    [Fact]
    public void GetFlashed_delegates_to_flash_service()
    {
        var flashService = Substitute.For<IInertiaFlashService>();
        flashService.GetPending().Returns(new Dictionary<string, object?> { ["key"] = "value" });
        var factory = new InertiaResponseFactory(Options.Create(new InertiaOptions()), flashService, Substitute.For<IHttpContextAccessor>());

        var result = factory.GetFlashed();

        Assert.Equal("value", result["key"]);
        flashService.Received(1).GetPending();
    }
}
