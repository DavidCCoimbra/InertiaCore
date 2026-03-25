namespace InertiaCore.Tests.Core.InertiaResponse;

[Trait("Method", "ExecuteAsync")]
public class HistoryFlagResponseTests : InertiaResponseTestBase
{
    [Fact]
    public async Task EncryptHistory_true_appears_in_page()
    {
        var response = new InertiaCore.Core.InertiaResponse(
            "Test", new(), new(), "App", null, encryptHistory: true);
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.True(page.ContainsKey("encryptHistory"));
        Assert.True(page["encryptHistory"].GetBoolean());
    }

    [Fact]
    public async Task EncryptHistory_false_not_in_page()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.False(page.ContainsKey("encryptHistory"));
    }

    [Fact]
    public async Task ClearHistory_true_appears_in_page()
    {
        var response = new InertiaCore.Core.InertiaResponse(
            "Test", new(), new(), "App", null, clearHistory: true);
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.True(page.ContainsKey("clearHistory"));
        Assert.True(page["clearHistory"].GetBoolean());
    }

    [Fact]
    public async Task ClearHistory_false_not_in_page()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.False(page.ContainsKey("clearHistory"));
    }

    [Fact]
    public async Task PreserveFragment_true_appears_in_page()
    {
        var response = new InertiaCore.Core.InertiaResponse(
            "Test", new(), new(), "App", null, preserveFragment: true);
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.True(page.ContainsKey("preserveFragment"));
        Assert.True(page["preserveFragment"].GetBoolean());
    }

    [Fact]
    public async Task PreserveFragment_false_not_in_page()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.False(page.ContainsKey("preserveFragment"));
    }
}
