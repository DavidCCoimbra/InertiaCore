using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Models;

namespace InertiaCore.Vite.Services;

/// <summary>
/// Resolves Vite asset URLs for both development and production modes.
/// </summary>
public interface IViteAssetResolver
{
    /// <summary>
    /// Gets the current Vite configuration options.
    /// </summary>
    ViteOptions Options { get; }

    /// <summary>
    /// Gets the default entry points from configuration.
    /// </summary>
    string[] DefaultEntryPoints { get; }

    /// <summary>
    /// Returns whether the Vite dev server is currently running.
    /// </summary>
    bool IsDevServerRunning();

    /// <summary>
    /// Gets the Vite dev server URL from the hot file.
    /// </summary>
    string GetDevServerUrl();

    /// <summary>
    /// Resolves a Vite entry point to its hashed asset paths from the manifest.
    /// </summary>
    ResolvedAssets ResolveEntrypoint(string entrypoint);

    /// <summary>
    /// Resolves any asset path to its hashed URL from the manifest.
    /// </summary>
    string GetAssetUrl(string path);
}
