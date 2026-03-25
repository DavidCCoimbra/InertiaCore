using InertiaCore.Core;
using NSubstitute;

namespace InertiaCore.Tests.Core.InertiaResponse;

[Trait("Method", "ExecuteAsync")]
public class FlashMergeTests : InertiaResponseTestBase
{
    [Fact]
    public async Task Flash_data_appears_in_props()
    {
        var flashService = Substitute.For<IInertiaFlashService>();
        flashService.Consume().Returns(new Dictionary<string, object?> { ["success"] = "Done!" });

        var response = CreateResponse(flashService: flashService);
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        var flash = page["props"].GetProperty("flash");
        Assert.Equal("Done!", flash.GetProperty("success").GetString());
    }

    [Fact]
    public async Task No_flash_key_when_consume_returns_null()
    {
        var flashService = Substitute.For<IInertiaFlashService>();
        flashService.Consume().Returns((Dictionary<string, object?>?)null);

        var response = CreateResponse(flashService: flashService);
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.False(page["props"].TryGetProperty("flash", out _));
    }

    [Fact]
    public async Task No_flash_key_without_flash_service()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.False(page["props"].TryGetProperty("flash", out _));
    }
}
