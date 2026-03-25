using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class FlashIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public FlashIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Flash_page_renders_without_flash_initially()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/flash");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var page = await JsonSerializer.DeserializeAsync<JsonElement>(
            await response.Content.ReadAsStreamAsync());

        Assert.False(page.TryGetProperty("flash", out _));
    }

    [Fact]
    public async Task Post_flash_returns_redirect()
    {
        var client = _factory.CreateClient();

        // Default client follows redirects, so the final response is the GET after redirect
        var request = new HttpRequestMessage(HttpMethod.Post, "/flash");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await client.SendAsync(request);

        // Successfully followed redirect to GET /flash
        response.EnsureSuccessStatusCode();
    }
}
