namespace InertiaCore.Vite.Services;

/// <summary>
/// Detects whether the Vite dev server is running by reading the hot file.
/// </summary>
public interface IViteDevServerDetector
{
    /// <summary>
    /// Returns whether the Vite dev server is currently running.
    /// </summary>
    bool IsRunning();

    /// <summary>
    /// Gets the Vite dev server URL from the hot file.
    /// </summary>
    string GetUrl();
}
