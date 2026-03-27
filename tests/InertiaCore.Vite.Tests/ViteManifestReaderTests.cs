using System.Text.Json;
using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Models;
using InertiaCore.Vite.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Vite.Tests;

[Trait("Class", "ViteManifestReader")]
public class ViteManifestReaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _webRoot;

    public ViteManifestReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"vite-test-{Guid.NewGuid():N}");
        _webRoot = Path.Combine(_tempDir, "wwwroot");
        Directory.CreateDirectory(Path.Combine(_webRoot, "build"));
    }

    [Fact]
    public void Resolves_simple_entry_point()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["resources/js/app.ts"] = new()
            {
                File = "assets/app-abc123.js",
                IsEntry = true,
            },
        });

        var reader = CreateReader();
        var result = reader.ResolveEntrypoint("resources/js/app.ts");

        Assert.Equal("build/assets/app-abc123.js", result.JsFile);
        Assert.Empty(result.CssFiles);
        Assert.Empty(result.PreloadFiles);
    }

    [Fact]
    public void Resolves_entry_with_css()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["resources/js/app.ts"] = new()
            {
                File = "assets/app-abc123.js",
                IsEntry = true,
                Css = ["assets/app-def456.css"],
            },
        });

        var reader = CreateReader();
        var result = reader.ResolveEntrypoint("resources/js/app.ts");

        Assert.Single(result.CssFiles);
        Assert.Equal("build/assets/app-def456.css", result.CssFiles[0]);
    }

    [Fact]
    public void Resolves_entry_with_imports()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["resources/js/app.ts"] = new()
            {
                File = "assets/app-abc123.js",
                IsEntry = true,
                Imports = ["resources/js/vendor.ts"],
            },
            ["resources/js/vendor.ts"] = new()
            {
                File = "assets/vendor-xyz789.js",
            },
        });

        var reader = CreateReader();
        var result = reader.ResolveEntrypoint("resources/js/app.ts");

        Assert.Single(result.PreloadFiles);
        Assert.Equal("build/assets/vendor-xyz789.js", result.PreloadFiles[0]);
    }

    [Fact]
    public void Collects_css_from_nested_imports()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["resources/js/app.ts"] = new()
            {
                File = "assets/app.js",
                IsEntry = true,
                Imports = ["resources/js/components.ts"],
            },
            ["resources/js/components.ts"] = new()
            {
                File = "assets/components.js",
                Css = ["assets/components.css"],
            },
        });

        var reader = CreateReader();
        var result = reader.ResolveEntrypoint("resources/js/app.ts");

        Assert.Contains("build/assets/components.css", result.CssFiles);
    }

    [Fact]
    public void Prevents_infinite_loops_on_circular_imports()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["a.ts"] = new()
            {
                File = "a.js",
                IsEntry = true,
                Imports = ["b.ts"],
            },
            ["b.ts"] = new()
            {
                File = "b.js",
                Imports = ["a.ts"],
            },
        });

        var reader = CreateReader();

        // Should not throw / infinite loop
        var result = reader.ResolveEntrypoint("a.ts");

        Assert.Equal("build/a.js", result.JsFile);
    }

    [Fact]
    public void Skips_imports_not_found_in_manifest()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["app.ts"] = new()
            {
                File = "app.js",
                IsEntry = true,
                Imports = ["missing-chunk.ts", "vendor.ts"],
            },
            ["vendor.ts"] = new()
            {
                File = "vendor.js",
            },
        });

        var reader = CreateReader();
        var result = reader.ResolveEntrypoint("app.ts");

        // missing-chunk.ts skipped, vendor.ts collected
        Assert.Single(result.PreloadFiles);
        Assert.Equal("build/vendor.js", result.PreloadFiles[0]);
    }

    [Fact]
    public async Task Concurrent_reads_return_same_manifest()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["app.ts"] = new() { File = "app.js", IsEntry = true },
        });

        var reader = CreateReader();

        // Simulate concurrent access — both should get same result
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => reader.ResolveEntrypoint("app.ts")))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.All(tasks, t => Assert.Equal("build/app.js", t.Result.JsFile));
    }

    // -- GetAssetUrl --

    [Fact]
    public void GetAssetUrl_resolves_image()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["resources/images/logo.png"] = new() { File = "assets/logo-abc123.png" },
        });

        var reader = CreateReader();
        var url = reader.GetAssetUrl("resources/images/logo.png");

        Assert.Equal("/build/assets/logo-abc123.png", url);
    }

    [Fact]
    public void GetAssetUrl_resolves_font()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["resources/fonts/Inter.woff2"] = new() { File = "assets/Inter-xyz789.woff2" },
        });

        var reader = CreateReader();
        var url = reader.GetAssetUrl("resources/fonts/Inter.woff2");

        Assert.Equal("/build/assets/Inter-xyz789.woff2", url);
    }

    [Fact]
    public void GetAssetUrl_throws_on_missing_asset()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["resources/images/logo.png"] = new() { File = "assets/logo.png" },
        });

        var reader = CreateReader();

        var ex = Assert.Throws<FileNotFoundException>(
            () => reader.GetAssetUrl("resources/images/missing.png"));

        Assert.Contains("missing.png", ex.Message);
    }

    [Fact]
    public void Throws_on_invalid_manifest_json()
    {
        var manifestPath = Path.Combine(_webRoot, "build", "manifest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllText(manifestPath, "null");

        var reader = CreateReader();

        Assert.Throws<InvalidOperationException>(() => reader.ResolveEntrypoint("app.ts"));
    }

    [Fact]
    public void Throws_on_missing_entry_point()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["resources/js/app.ts"] = new() { File = "assets/app.js" },
        });

        var reader = CreateReader();

        var ex = Assert.Throws<FileNotFoundException>(
            () => reader.ResolveEntrypoint("nonexistent.ts"));

        Assert.Contains("nonexistent.ts", ex.Message);
    }

    [Fact]
    public void Throws_when_manifest_file_missing()
    {
        // Don't write any manifest
        var reader = CreateReader();

        Assert.Throws<FileNotFoundException>(
            () => reader.ResolveEntrypoint("app.ts"));
    }

    [Fact]
    public void Caches_manifest_after_first_read()
    {
        WriteManifest(new Dictionary<string, ManifestEntry>
        {
            ["app.ts"] = new() { File = "app.js", IsEntry = true },
        });

        var reader = CreateReader();

        var result1 = reader.ResolveEntrypoint("app.ts");
        var result2 = reader.ResolveEntrypoint("app.ts");

        Assert.Equal(result1.JsFile, result2.JsFile);
    }

    private ViteManifestReader CreateReader()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(_webRoot);

        var options = Options.Create(new ViteOptions());
        return new ViteManifestReader(env, options);
    }

    private void WriteManifest(Dictionary<string, ManifestEntry> manifest)
    {
        var path = Path.Combine(_webRoot, "build", "manifest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(manifest));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
