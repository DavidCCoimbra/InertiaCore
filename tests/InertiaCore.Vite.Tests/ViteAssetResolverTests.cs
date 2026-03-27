using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Models;
using InertiaCore.Vite.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Vite.Tests;

[Trait("Class", "ViteAssetResolver")]
public class ViteAssetResolverTests
{
    [Fact]
    public void IsDevServerRunning_delegates_to_detector()
    {
        var detector = Substitute.For<IViteDevServerDetector>();
        detector.IsRunning().Returns(true);

        var resolver = CreateResolver(detector: detector);

        Assert.True(resolver.IsDevServerRunning());
        detector.Received(1).IsRunning();
    }

    [Fact]
    public void GetDevServerUrl_delegates_to_detector()
    {
        var detector = Substitute.For<IViteDevServerDetector>();
        detector.GetUrl().Returns("http://localhost:5173");

        var resolver = CreateResolver(detector: detector);

        Assert.Equal("http://localhost:5173", resolver.GetDevServerUrl());
    }

    [Fact]
    public void ResolveEntrypoint_delegates_to_manifest_reader()
    {
        var expected = new ResolvedAssets("build/app.js", ["build/app.css"], []);
        var reader = Substitute.For<IViteManifestReader>();
        reader.ResolveEntrypoint("app.ts").Returns(expected);

        var resolver = CreateResolver(reader: reader);
        var result = resolver.ResolveEntrypoint("app.ts");

        Assert.Same(expected, result);
    }

    [Fact]
    public void DefaultEntryPoints_returns_configured_options()
    {
        var resolver = CreateResolver(entryPoints: ["custom/entry.ts"]);

        Assert.Equal(["custom/entry.ts"], resolver.DefaultEntryPoints);
    }

    [Fact]
    public void GetAssetUrl_delegates_to_manifest_reader()
    {
        var reader = Substitute.For<IViteManifestReader>();
        reader.GetAssetUrl("images/logo.png").Returns("/build/assets/logo-abc123.png");

        var resolver = CreateResolver(reader: reader);
        var url = resolver.GetAssetUrl("images/logo.png");

        Assert.Equal("/build/assets/logo-abc123.png", url);
    }

    [Fact]
    public void Options_exposes_configuration()
    {
        var resolver = CreateResolver(entryPoints: ["app.ts"]);

        Assert.NotNull(resolver.Options);
        Assert.Equal("build", resolver.Options.BuildDirectory);
    }

    private static ViteAssetResolver CreateResolver(
        IViteDevServerDetector? detector = null,
        IViteManifestReader? reader = null,
        string[]? entryPoints = null)
    {
        var options = new ViteOptions();
        if (entryPoints is not null)
        {
            options.EntryPoints = entryPoints;
        }

        return new ViteAssetResolver(
            Options.Create(options),
            detector ?? Substitute.For<IViteDevServerDetector>(),
            reader ?? Substitute.For<IViteManifestReader>());
    }
}
