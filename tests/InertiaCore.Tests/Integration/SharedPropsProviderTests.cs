using InertiaCore.Contracts;
using InertiaCore.Testing;
using InertiaCore.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class SharedPropsProviderTests
{
    [Fact]
    public async Task Provider_props_appear_in_response()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.AddScoped<ISharedPropsProvider, TestSharedPropsProvider>();
                });
            });

        var client = factory.CreateClient();
        var inertia = await client.GetInertiaAssertAsync("/", "1.0.0");

        inertia
            .HasProp("appName", "TestApp")
            .HasProp("environment", "Testing");
    }

    [Fact]
    public async Task Multiple_providers_merged()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.AddScoped<ISharedPropsProvider, TestSharedPropsProvider>();
                    services.AddScoped<ISharedPropsProvider, SecondSharedPropsProvider>();
                });
            });

        var client = factory.CreateClient();
        var inertia = await client.GetInertiaAssertAsync("/", "1.0.0");

        inertia
            .HasProp("appName", "TestApp")
            .HasProp("extraProp", "fromSecondProvider");
    }

    [Fact]
    public async Task Per_request_share_overrides_provider()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.AddScoped<ISharedPropsProvider, TestSharedPropsProvider>();
                });
            });

        var client = factory.CreateClient();
        // /shared endpoint calls inertia.Share("appName", "InertiaCore DemoApp")
        // which should override the provider's "TestApp"
        var inertia = await client.GetInertiaAssertAsync("/shared", "1.0.0");

        inertia.HasProp("appName", "InertiaCore DemoApp");
    }

    private sealed class TestSharedPropsProvider : ISharedPropsProvider
    {
        public Dictionary<string, object?> GetSharedProps(HttpContext context) => new()
        {
            ["appName"] = "TestApp",
            ["environment"] = "Testing",
        };
    }

    private sealed class SecondSharedPropsProvider : ISharedPropsProvider
    {
        public Dictionary<string, object?> GetSharedProps(HttpContext context) => new()
        {
            ["extraProp"] = "fromSecondProvider",
        };
    }
}
