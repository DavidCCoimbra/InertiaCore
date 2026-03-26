using InertiaCore.Testing;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class MapInertiaTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public MapInertiaTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task MapInertia_renders_component()
    {
        var inertia = await _client.GetInertiaAssertAsync("/about", "1.0.0");

        inertia
            .HasComponent("About/Index")
            .HasUrl("/about");
    }

    [Fact]
    public async Task MapInertia_includes_static_props()
    {
        var inertia = await _client.GetInertiaAssertAsync("/about", "1.0.0");

        inertia.HasProp("Title", "About Us");
    }

    [Fact]
    public async Task MapInertia_returns_html_for_browser()
    {
        var response = await _client.GetAsync("/about");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-page=", html);
        Assert.Contains("About/Index", html);
    }
}
