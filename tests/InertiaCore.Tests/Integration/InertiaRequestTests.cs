using System.Net;
using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class InertiaRequestTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public InertiaRequestTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Returns_json_page_object()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var page = await JsonSerializer.DeserializeAsync<JsonElement>(
            await response.Content.ReadAsStreamAsync());
        Assert.Equal("Home/Index", page.GetProperty("component").GetString());
        Assert.Equal("/", page.GetProperty("url").GetString());
        Assert.Equal("1.0.0", page.GetProperty("version").GetString());
        Assert.Equal("Hello from Inertia!", page.GetProperty("props").GetProperty("greeting").GetString());
    }

    [Fact]
    public async Task Sets_x_inertia_response_header()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.Contains(InertiaHeaders.Inertia));
    }

    [Fact]
    public async Task Version_mismatch_returns_409()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "old-version");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(response.Headers.Contains(InertiaHeaders.Location));
    }

    [Fact]
    public async Task Converts_302_to_303_for_put()
    {
        using var noRedirectClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Put, "/redirect");
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.Version, "1.0.0");

        var response = await noRedirectClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
    }
}
