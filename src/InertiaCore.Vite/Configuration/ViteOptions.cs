namespace InertiaCore.Vite.Configuration;

/// <summary>
/// Configuration options for Vite asset integration.
/// </summary>
public class ViteOptions
{
    /// <summary>
    /// Path to the Vite manifest file, relative to wwwroot.
    /// </summary>
    public string ManifestPath { get; set; } = "build/.vite/manifest.json";

    /// <summary>
    /// Path to the hot file written by the Vite dev server, relative to wwwroot.
    /// </summary>
    public string HotFilePath { get; set; } = "hot";

    /// <summary>
    /// Vite entry points to include when emitting script/link tags.
    /// </summary>
    public string[] EntryPoints { get; set; } = ["resources/js/app.ts"];

    /// <summary>
    /// Build output subdirectory within wwwroot.
    /// </summary>
    public string BuildDirectory { get; set; } = "build";

    /// <summary>
    /// Whether to inject the React refresh preamble script in development mode.
    /// </summary>
    public bool ReactRefresh { get; set; }
}
