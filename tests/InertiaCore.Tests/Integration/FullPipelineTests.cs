using InertiaCore.Testing;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Integration;

/// <summary>
/// End-to-end integration tests exercising features from all phases together.
/// </summary>
[Trait("Category", "Integration")]
public class FullPipelineTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public FullPipelineTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Full_pipeline_initial_page_load()
    {
        // Phase 1: Browser request returns HTML with data-page
        var response = await _client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-page=", html);
        Assert.Contains("Home/Index", html);
    }

    [Fact]
    public async Task Full_pipeline_inertia_json_response()
    {
        // Phase 1: Inertia request returns JSON page object
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia
            .IsSuccessful()
            .IsInertiaResponse()
            .HasComponent("Home/Index")
            .HasUrl("/")
            .HasVersion("1.0.0")
            .HasProp("greeting", "Hello from Inertia!")
            .HasNoErrors()
            .HasNoFlash();
    }

    [Fact]
    public async Task Full_pipeline_prop_types_and_metadata()
    {
        // Phase 2: All prop types with metadata
        var inertia = await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

        inertia
            .HasComponent("Dashboard/Index")
            // AlwaysProp — always included
            .HasProp("user")
            // DeferProp — excluded, metadata collected
            .MissingProp("stats")
            .HasDeferredProp("stats", group: "analytics")
            // MergeProp — included with metadata
            .HasProp("items")
            .HasMergedProp("items")
            // OptionalProp — excluded (IIgnoreFirstLoad)
            .MissingProp("lazy")
            // OnceProp — excluded (IIgnoreFirstLoad)
            .MissingProp("permissions")
            // Shared errors from middleware
            .HasSharedProp("errors")
            .HasNoErrors();
    }

    [Fact]
    public async Task Full_pipeline_partial_reload()
    {
        // Phase 2: Partial reload with only filter
        var inertia = await ReloadRequest.For(_client, "/dashboard")
            .Component("Dashboard/Index")
            .Version("1.0.0")
            .Only("stats", "user")
            .SendAndAssertAsync();

        inertia
            .HasComponent("Dashboard/Index")
            // AlwaysProp bypasses filter
            .HasProp("user")
            // DeferProp included when requested
            .HasProp("stats")
            // MergeProp excluded (not in only list)
            .MissingProp("items");
    }

    [Fact]
    public async Task Full_pipeline_flash_redirect_cycle()
    {
        // Phase 3: POST flash → redirect → consume
        var postResponse = await _client.PostInertiaAsync("/flash", version: "1.0.0");
        postResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Full_pipeline_version_mismatch()
    {
        // Phase 1: Version mismatch returns 409
        var response = await _client.GetInertiaAsync("/", "wrong-version");

        Assert.Equal(409, (int)response.StatusCode);
    }

    [Fact]
    public async Task Full_pipeline_map_inertia_shortcut()
    {
        // Phase 6: MapInertia route shortcut
        var inertia = await _client.GetInertiaAssertAsync("/about", "1.0.0");

        inertia
            .HasComponent("About/Index")
            .HasProp("title", "About Us");
    }

    [Fact]
    public async Task Full_pipeline_scroll_prop()
    {
        // Phase 3: ScrollProp with pagination metadata
        var inertia = await _client.GetInertiaAssertAsync("/scroll", "1.0.0");

        inertia
            .HasComponent("Scroll/Index")
            .HasProp("items")
            .HasProp("totalPages");
    }

    [Fact]
    public async Task Full_pipeline_all_assertions_chain()
    {
        // Phase 5: Testing utilities fluent chain
        var inertia = await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

        inertia
            .IsSuccessful()
            .IsInertiaResponse()
            .HasComponent("Dashboard/Index")
            .HasUrl("/dashboard")
            .HasVersion("1.0.0")
            .HasProp("user")
            .HasDeferredProp("stats")
            .HasMergedProp("items")
            .HasSharedProp("errors")
            .HasNoErrors()
            .HasNoFlash()
            .MissingProp("nonexistent")
            .MissingMetadata("nonexistent");
    }
}
