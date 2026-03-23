using System.Text.Json;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class BrowserRequestTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public BrowserRequestTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Returns_html_with_data_page_attribute()
    {
        var response = await _client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-page=", html);
    }

    [Fact]
    public async Task Data_page_contains_component_and_props()
    {
        var response = await _client.GetAsync("/");

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Home/Index", html);
        Assert.Contains("Hello from Inertia!", html);
    }

    [Fact]
    public async Task Includes_vary_header()
    {
        var response = await _client.GetAsync("/");

        Assert.Contains("X-Inertia", response.Headers.GetValues("Vary"));
    }

    [Fact]
    public async Task Non_inertia_api_returns_json_directly()
    {
        var response = await _client.GetAsync("/api/health");

        response.EnsureSuccessStatusCode();
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(
            await response.Content.ReadAsStreamAsync());
        Assert.Equal("ok", json.GetProperty("status").GetString());
    }
}
