using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Extensions;
using InertiaCore.Vite.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Vite.Tests;

[Trait("Class", "ServiceRegistration")]
public class ServiceRegistrationTests
{
    [Fact]
    public void AddVite_registers_all_services()
    {
        var services = CreateServices();
        services.AddVite();
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IViteManifestReader>());
        Assert.NotNull(provider.GetService<IViteDevServerDetector>());
        Assert.NotNull(provider.GetService<IViteAssetResolver>());
    }

    [Fact]
    public void AddVite_with_configure_applies_options()
    {
        var services = CreateServices();
        services.AddVite(o => o.EntryPoints = ["custom/app.ts"]);
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ViteOptions>>();

        Assert.Equal(["custom/app.ts"], options.Value.EntryPoints);
    }

    [Fact]
    public void ManifestReader_is_singleton()
    {
        var services = CreateServices();
        services.AddVite();
        var provider = services.BuildServiceProvider();

        var r1 = provider.GetRequiredService<IViteManifestReader>();
        var r2 = provider.GetRequiredService<IViteManifestReader>();

        Assert.Same(r1, r2);
    }

    [Fact]
    public void DevServerDetector_is_singleton()
    {
        var services = CreateServices();
        services.AddVite();
        var provider = services.BuildServiceProvider();

        var d1 = provider.GetRequiredService<IViteDevServerDetector>();
        var d2 = provider.GetRequiredService<IViteDevServerDetector>();

        Assert.Same(d1, d2);
    }

    [Fact]
    public void AssetResolver_is_scoped()
    {
        var services = CreateServices();
        services.AddVite();
        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var a1 = scope1.ServiceProvider.GetRequiredService<IViteAssetResolver>();
        var a2 = scope2.ServiceProvider.GetRequiredService<IViteAssetResolver>();

        Assert.NotSame(a1, a2);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(CreateMockEnv());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        return services;
    }

    private static IWebHostEnvironment CreateMockEnv()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(Path.GetTempPath());
        env.EnvironmentName.Returns("Development");
        return env;
    }
}
