using System.Text.Json;
using InertiaCore.Ssr;
using InertiaCore.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class SsrHealthCheckTests
{
    [Fact]
    public async Task Returns_disabled_when_no_gateway()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISsrGateway));
                    if (descriptor != null) services.Remove(descriptor);
                });
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ssr");

        response.EnsureSuccessStatusCode();
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(
            await response.Content.ReadAsStreamAsync());
        Assert.Equal("disabled", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Returns_healthy_when_gateway_healthy()
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(true);

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services => services.AddSingleton(gateway));
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ssr");

        response.EnsureSuccessStatusCode();
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(
            await response.Content.ReadAsStreamAsync());
        Assert.Equal("healthy", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Returns_unhealthy_when_gateway_unhealthy()
    {
        var gateway = Substitute.For<ISsrGateway>();
        gateway.IsHealthyAsync(Arg.Any<CancellationToken>()).Returns(false);

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services => services.AddSingleton(gateway));
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/ssr");

        Assert.Equal(503, (int)response.StatusCode);
    }
}
