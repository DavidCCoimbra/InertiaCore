using InertiaCore.Constants;
using InertiaCore.Testing;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Testing;

[Trait("Class", "AssertableInertia")]
public class AssertableInertiaTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public AssertableInertiaTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    // -- FromResponse: JSON --

    [Fact]
    public async Task FromResponse_parses_json_response()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        Assert.Equal("Home/Index", inertia.Component);
        Assert.Equal("/", inertia.Url);
        Assert.Equal("1.0.0", inertia.Version);
    }

    // -- FromResponse: HTML --

    [Fact]
    public async Task FromResponse_parses_html_response()
    {
        var response = await _client.GetAsync("/");
        var inertia = await AssertableInertia.FromResponseAsync(response);

        Assert.Equal("Home/Index", inertia.Component);
    }

    // -- HasComponent --

    [Fact]
    public async Task HasComponent_passes_on_match()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.HasComponent("Home/Index");
    }

    [Fact]
    public async Task HasComponent_throws_on_mismatch()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        Assert.Throws<AssertionException>(() => inertia.HasComponent("Wrong/Component"));
    }

    // -- HasUrl --

    [Fact]
    public async Task HasUrl_passes_on_match()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.HasUrl("/");
    }

    // -- HasVersion --

    [Fact]
    public async Task HasVersion_passes_on_match()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.HasVersion("1.0.0");
    }

    // -- HasProp --

    [Fact]
    public async Task HasProp_with_value_passes()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.HasProp("greeting", "Hello from Inertia!");
    }

    [Fact]
    public async Task HasProp_exists_passes()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.HasProp("greeting");
    }

    [Fact]
    public async Task HasProp_throws_when_missing()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        Assert.Throws<AssertionException>(() => inertia.HasProp("nonexistent"));
    }

    // -- HasProp<T> --

    [Fact]
    public async Task HasProp_typed_callback()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.HasProp<string>("greeting", value =>
        {
            Assert.NotNull(value);
            Assert.Contains("Inertia", value!);
        });
    }

    // -- MissingProp --

    [Fact]
    public async Task MissingProp_passes_when_absent()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.MissingProp("nonexistent");
    }

    [Fact]
    public async Task MissingProp_throws_when_present()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        Assert.Throws<AssertionException>(() => inertia.MissingProp("greeting"));
    }

    // -- Fluent chaining --

    [Fact]
    public async Task Fluent_chain_works()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia
            .HasComponent("Home/Index")
            .HasUrl("/")
            .HasVersion("1.0.0")
            .HasProp("greeting", "Hello from Inertia!")
            .MissingProp("nonexistent");
    }

    // -- Metadata --

    [Fact]
    public async Task HasMetadata_for_deferred_props()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);
        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia
            .HasComponent("Dashboard/Index")
            .HasMetadata("deferredProps")
            .HasMetadata("mergeProps");
    }

    // -- Deferred props --

    [Fact]
    public async Task HasDeferredProp_passes()
    {
        var inertia = await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

        inertia.HasDeferredProp("stats", group: "analytics");
    }

    [Fact]
    public async Task HasDeferredProp_without_group()
    {
        var inertia = await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

        inertia.HasDeferredProp("stats");
    }

    [Fact]
    public async Task HasDeferredProp_throws_when_not_deferred()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        Assert.Throws<AssertionException>(() => inertia.HasDeferredProp("greeting"));
    }

    // -- Merge props --

    [Fact]
    public async Task HasMergedProp_passes()
    {
        var inertia = await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

        inertia.HasMergedProp("items");
    }

    [Fact]
    public async Task HasMergedProp_throws_when_not_merged()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        Assert.Throws<AssertionException>(() => inertia.HasMergedProp("greeting"));
    }

    // -- Errors --

    [Fact]
    public async Task HasNoErrors_passes_on_initial_load()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia.HasNoErrors();
    }

    [Fact]
    public async Task HasError_throws_when_no_errors()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        Assert.Throws<AssertionException>(() => inertia.HasError("name"));
    }

    // -- Flash --

    [Fact]
    public async Task HasNoFlash_passes_on_initial_load()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia.HasNoFlash();
    }

    [Fact]
    public async Task HasFlash_throws_when_no_flash()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        Assert.Throws<AssertionException>(() => inertia.HasFlash("success"));
    }

    // -- PropCount --

    [Fact]
    public async Task PropCount_passes_on_match()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        // Home/Index has: Greeting + errors (shared)
        inertia.PropCount(2);
    }

    [Fact]
    public async Task PropCount_throws_on_mismatch()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        Assert.Throws<AssertionException>(() => inertia.PropCount(99));
    }

    // -- Shared props --

    [Fact]
    public async Task HasSharedProp_passes()
    {
        var inertia = await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

        inertia.HasSharedProp("errors");
    }

    [Fact]
    public async Task HasSharedProp_throws_when_not_shared()
    {
        var inertia = await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

        Assert.Throws<AssertionException>(() => inertia.HasSharedProp("nonexistent"));
    }

    // -- Where predicate --

    [Fact]
    public async Task Where_passes_with_matching_predicate()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia.Where("greeting", prop => prop.GetString()!.Contains("Inertia"));
    }

    [Fact]
    public async Task Where_throws_on_failing_predicate()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        Assert.Throws<AssertionException>(() =>
            inertia.Where("greeting", prop => prop.GetString() == "wrong"));
    }

    // -- HasPropValue typed --

    [Fact]
    public async Task HasPropValue_string_passes()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia.HasPropValue("greeting", "Hello from Inertia!");
    }

    [Fact]
    public async Task HasPropValue_throws_on_mismatch()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        Assert.Throws<AssertionException>(() =>
            inertia.HasPropValue("greeting", "wrong value"));
    }

    // -- Response-level --

    [Fact]
    public async Task IsInertiaResponse_passes()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia.IsInertiaResponse();
    }

    [Fact]
    public async Task IsSuccessful_passes()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia.IsSuccessful();
    }

    // -- Full fluent chain with all methods --

    [Fact]
    public async Task Full_fluent_chain_with_all_assertions()
    {
        var inertia = await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

        inertia
            .IsSuccessful()
            .IsInertiaResponse()
            .HasComponent("Dashboard/Index")
            .HasUrl("/dashboard")
            .HasVersion("1.0.0")
            .HasDeferredProp("stats", group: "analytics")
            .HasMergedProp("items")
            .HasProp("user")
            .HasSharedProp("errors")
            .HasNoErrors()
            .HasNoFlash();
    }
}
