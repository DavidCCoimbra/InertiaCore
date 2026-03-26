using InertiaCore.Testing;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Testing;

[Trait("Class", "ReloadRequest")]
public class ReloadRequestTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public ReloadRequestTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SendAndAssertAsync_returns_assertable()
    {
        var inertia = await ReloadRequest.For(_client, "/dashboard")
            .Component("Dashboard/Index")
            .Version("1.0.0")
            .Only("user")
            .SendAndAssertAsync();

        inertia
            .HasComponent("Dashboard/Index")
            .HasProp("user");
    }

    [Fact]
    public async Task Only_filters_to_requested_props()
    {
        var inertia = await ReloadRequest.For(_client, "/dashboard")
            .Component("Dashboard/Index")
            .Version("1.0.0")
            .Only("user")
            .SendAndAssertAsync();

        inertia.HasProp("user");
    }

    [Fact]
    public async Task Except_excludes_specified_props()
    {
        var inertia = await ReloadRequest.For(_client, "/dashboard")
            .Component("Dashboard/Index")
            .Version("1.0.0")
            .Except("user")
            .SendAndAssertAsync();

        inertia.HasComponent("Dashboard/Index");
    }

    [Fact]
    public async Task Reset_sets_reset_header()
    {
        var inertia = await ReloadRequest.For(_client, "/dashboard")
            .Component("Dashboard/Index")
            .Version("1.0.0")
            .Only("items")
            .Reset("items")
            .SendAndAssertAsync();

        inertia.HasComponent("Dashboard/Index");
    }

    [Fact]
    public void Only_and_except_are_mutually_exclusive()
    {
        var builder = ReloadRequest.For(_client, "/dashboard")
            .Component("Dashboard/Index")
            .Only("user");

        Assert.Throws<InvalidOperationException>(() => builder.Except("items"));
    }

    [Fact]
    public void Except_then_only_throws()
    {
        var builder = ReloadRequest.For(_client, "/dashboard")
            .Component("Dashboard/Index")
            .Except("user");

        Assert.Throws<InvalidOperationException>(() => builder.Only("items"));
    }

    [Fact]
    public async Task Missing_component_throws()
    {
        var builder = ReloadRequest.For(_client, "/dashboard")
            .Version("1.0.0");

        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.SendAsync());
    }

    [Fact]
    public async Task SendAsync_returns_raw_response()
    {
        var response = await ReloadRequest.For(_client, "/dashboard")
            .Component("Dashboard/Index")
            .Version("1.0.0")
            .Only("user")
            .SendAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
