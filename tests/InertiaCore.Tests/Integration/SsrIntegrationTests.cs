using InertiaCore.Ssr;
using InertiaCore.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class SsrIntegrationTests
{
    [Fact]
    public async Task Browser_request_with_ssr_renders_ssr_body()
    {
        var ssrGateway = Substitute.For<ISsrGateway>();
        ssrGateway.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(new SsrResponse(
                Head: "<title>SSR Title</title>",
                Body: "<h1>Server Rendered</h1>"));

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(ssrGateway);
                });
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Server Rendered", html);
        Assert.Contains("data-page=", html);
    }

    [Fact]
    public async Task Browser_request_without_ssr_renders_csr_div()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove SSR gateway so it's null
                    var descriptor = services.FirstOrDefault(
                        d => d.ServiceType == typeof(ISsrGateway));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                });
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("data-page=", html);
        Assert.DoesNotContain("Server Rendered", html);
    }

    [Fact]
    public async Task Ssr_failure_falls_back_to_csr()
    {
        var ssrGateway = Substitute.For<ISsrGateway>();
        ssrGateway.RenderAsync(Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns<SsrResponse?>(_ => throw new HttpRequestException("SSR sidecar down"));

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(ssrGateway);
                });
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Falls back to CSR — data-page present, no SSR content
        Assert.Contains("data-page=", html);
    }
}
