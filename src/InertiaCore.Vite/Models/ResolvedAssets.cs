namespace InertiaCore.Vite.Models;

/// <summary>
/// Resolved asset paths for a single Vite entrypoint.
/// </summary>
public record ResolvedAssets(string JsFile, string[] CssFiles, string[] PreloadFiles);
