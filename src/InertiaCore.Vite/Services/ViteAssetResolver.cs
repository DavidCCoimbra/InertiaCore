using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Models;
using Microsoft.Extensions.Options;

namespace InertiaCore.Vite.Services;

/// <inheritdoc />
public sealed class ViteAssetResolver(
    IOptions<ViteOptions> options,
    IViteDevServerDetector detector,
    IViteManifestReader manifestReader) : IViteAssetResolver
{
    /// <inheritdoc />
    public ViteOptions Options => options.Value;

    /// <inheritdoc />
    public string[] DefaultEntryPoints => options.Value.EntryPoints;

    /// <inheritdoc />
    public bool IsDevServerRunning() => detector.IsRunning();

    /// <inheritdoc />
    public string GetDevServerUrl() => detector.GetUrl();

    /// <inheritdoc />
    public ResolvedAssets ResolveEntrypoint(string entrypoint) => manifestReader.ResolveEntrypoint(entrypoint);

    /// <inheritdoc />
    public string GetAssetUrl(string path) => manifestReader.GetAssetUrl(path);
}
