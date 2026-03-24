using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class PartialReloadIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public PartialReloadIntegrationTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Initial_load_excludes_deferred_and_optional_props()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);

        var page = await ReadPage(response);
        var props = page.GetProperty("props");

        // AlwaysProp included
        Assert.Equal("Alice", props.GetProperty("user").GetString());

        // MergeProp included (not IIgnoreFirstLoad)
        Assert.Equal(JsonValueKind.Array, props.GetProperty("items").ValueKind);

        // DeferProp excluded (IIgnoreFirstLoad)
        Assert.False(props.TryGetProperty("stats", out _));

        // OptionalProp excluded (IIgnoreFirstLoad)
        Assert.False(props.TryGetProperty("lazy", out _));
    }

    [Fact]
    public async Task Initial_load_includes_deferred_metadata()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);

        var page = await ReadPage(response);

        Assert.True(page.TryGetProperty("deferredProps", out var deferred));
        Assert.Equal(JsonValueKind.Object, deferred.ValueKind);
        Assert.True(deferred.TryGetProperty("analytics", out var analyticsGroup));
        Assert.Equal(JsonValueKind.Array, analyticsGroup.ValueKind);
        Assert.Contains(analyticsGroup.EnumerateArray(), e => e.GetString() == "stats");
    }

    [Fact]
    public async Task Initial_load_includes_merge_metadata()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);

        var page = await ReadPage(response);

        Assert.True(page.TryGetProperty("mergeProps", out var merge));
        Assert.Equal(JsonValueKind.Array, merge.ValueKind);
        Assert.Contains(merge.EnumerateArray(), e => e.GetString() == "items");
    }

    [Fact]
    public async Task Partial_reload_with_only_returns_requested_props()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");
        request.Headers.Add(InertiaHeaders.PartialComponent, "Dashboard/Index");
        request.Headers.Add(InertiaHeaders.PartialOnly, "lazy");

        var response = await _client.SendAsync(request);

        var page = await ReadPage(response);
        var props = page.GetProperty("props");

        // Requested prop included and resolved
        Assert.Equal("optional-data", props.GetProperty("lazy").GetString());

        // AlwaysProp always included
        Assert.Equal("Alice", props.GetProperty("user").GetString());

        // Non-requested props excluded
        Assert.False(props.TryGetProperty("stats", out _));
        Assert.False(props.TryGetProperty("items", out _));
    }

    [Fact]
    public async Task Partial_reload_resolves_deferred_prop()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");
        request.Headers.Add(InertiaHeaders.PartialComponent, "Dashboard/Index");
        request.Headers.Add(InertiaHeaders.PartialOnly, "stats");

        var response = await _client.SendAsync(request);

        var page = await ReadPage(response);
        var props = page.GetProperty("props");

        Assert.Equal("heavy-stats", props.GetProperty("stats").GetString());
    }

    private static async Task<JsonElement> ReadPage(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream);
    }
}
