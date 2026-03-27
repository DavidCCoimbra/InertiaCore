using InertiaCore.Vite.Models;

namespace InertiaCore.Vite.Services;

/// <summary>
/// Parses the Vite manifest and resolves entry points to hashed asset paths.
/// </summary>
public interface IViteManifestReader
{
    /// <summary>
    /// Resolves an entry point to its hashed asset paths, including CSS and preload files.
    /// </summary>
    ResolvedAssets ResolveEntrypoint(string entryPoint);

    /// <summary>
    /// Resolves any asset path to its hashed URL from the Vite manifest.
    /// Useful for images, fonts, and other static assets referenced in Razor views.
    /// </summary>
    string GetAssetUrl(string path);
}
