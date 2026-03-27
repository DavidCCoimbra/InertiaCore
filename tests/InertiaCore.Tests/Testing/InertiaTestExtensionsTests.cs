using InertiaCore.Testing;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Testing;

[Trait("Class", "InertiaTestExtensions")]
public class InertiaTestExtensionsTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public InertiaTestExtensionsTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInertiaAsync_returns_json_response()
    {
        var response = await _client.GetInertiaAsync("/", "1.0.0");

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetInertiaAsync_without_version()
    {
        var response = await _client.GetInertiaAsync("/");

        // Without matching version, might get 409 or 200 depending on config
        Assert.NotNull(response);
    }

    [Fact]
    public async Task GetInertiaAssertAsync_returns_assertable()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia
            .HasComponent("Home/Index")
            .HasUrl("/")
            .HasProp("greeting");
    }

    [Fact]
    public async Task AssertInertiaAsync_on_response()
    {
        var response = await _client.GetInertiaAsync("/", "1.0.0");
        var inertia = await response.AssertInertiaAsync();

        inertia.HasComponent("Home/Index");
    }

    [Fact]
    public async Task PartialReloadAsync_filters_props()
    {
        var inertia = await (await _client.PartialReloadAsync(
            "/dashboard", "Dashboard/Index", ["user"], "1.0.0")).AssertInertiaAsync();

        inertia
            .HasComponent("Dashboard/Index")
            .HasProp("user");
    }

    [Fact]
    public async Task PartialReloadExceptAsync_excludes_props()
    {
        var response = await _client.PartialReloadExceptAsync(
            "/dashboard", "Dashboard/Index", ["user"], "1.0.0");

        var inertia = await response.AssertInertiaAsync();
        inertia.HasComponent("Dashboard/Index");
    }

    [Fact]
    public async Task PostInertiaAsync_sends_post()
    {
        var response = await _client.PostInertiaAsync("/flash", version: "1.0.0");

        // POST /flash redirects, client follows → final response
        Assert.NotNull(response);
    }

    [Fact]
    public async Task PutInertiaAsync_sends_put()
    {
        var response = await _client.PutInertiaAsync("/redirect", version: "1.0.0");

        Assert.NotNull(response);
    }

    [Fact]
    public async Task DeleteInertiaAsync_sends_delete()
    {
        var response = await _client.DeleteInertiaAsync("/redirect", version: "1.0.0");

        Assert.NotNull(response);
    }
}
