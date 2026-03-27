using System.Text.Json;
using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace InertiaCore.Vite.Services;

/// <inheritdoc />
public class ViteManifestReader(
    IWebHostEnvironment env,
    IOptions<ViteOptions> options) : IViteManifestReader
{
    private readonly Lock _lock = new();
    private Dictionary<string, ManifestEntry>? _manifest;

    /// <inheritdoc />
    public ResolvedAssets ResolveEntrypoint(string entryPoint)
    {
        var manifest = GetManifest();

        if (!manifest.TryGetValue(entryPoint, out var entry))
        {
            throw new FileNotFoundException(
                $"Entrypoint '{entryPoint}' not found in Vite manifest. " +
                "Did you run 'npm run build'?");
        }

        var buildDir = options.Value.BuildDirectory;
        var cssFiles = new List<string>();
        var preloadFiles = new List<string>();

        CollectAssets(manifest, entry, buildDir, cssFiles, preloadFiles, []);

        return new ResolvedAssets(
            JsFile: $"{buildDir}/{entry.File}",
            CssFiles: [.. cssFiles],
            PreloadFiles: [.. preloadFiles]);
    }

    private static void CollectAssets(
        Dictionary<string, ManifestEntry> manifest,
        ManifestEntry entry,
        string buildDir,
        List<string> css,
        List<string> preload,
        HashSet<string> visited)
    {
        if (!visited.Add(entry.File))
        {
            return;
        }

        foreach (var cssFile in entry.Css)
        {
            css.Add($"{buildDir}/{cssFile}");
        }

        foreach (var import in entry.Imports)
        {
            if (!manifest.TryGetValue(import, out var imported))
            {
                continue;
            }

            preload.Add($"{buildDir}/{imported.File}");
            CollectAssets(manifest, imported, buildDir, css, preload, visited);
        }
    }

    private Dictionary<string, ManifestEntry> GetManifest()
    {
        if (_manifest is not null)
        {
            return _manifest;
        }

        lock (_lock)
        {
            if (_manifest is not null)
            {
                return _manifest;
            }

            var manifestPath = Path.Combine(env.WebRootPath, options.Value.ManifestPath);

            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException(
                    $"Vite manifest not found at '{manifestPath}'. " +
                    "Run 'npm run build' to generate it.");
            }

            var json = File.ReadAllText(manifestPath);
            _manifest = JsonSerializer.Deserialize<Dictionary<string, ManifestEntry>>(json)
                ?? throw new InvalidOperationException("Failed to parse Vite manifest.");

            return _manifest;
        }
    }
}
