namespace InertiaCore.EmbeddedV8;

/// <summary>
/// Configuration options for the embedded V8 SSR engine.
/// </summary>
public sealed class EmbeddedSsrOptions
{
    /// <summary>
    /// Path to the Vite-built SSR bundle (e.g., "wwwroot/dist/ssr/ssr.mjs").
    /// </summary>
    public string BundlePath { get; set; } = "wwwroot/dist/ssr/ssr.mjs";

    /// <summary>
    /// Number of V8 engines in the pool. Each engine handles one concurrent render.
    /// Default: number of CPU cores.
    /// </summary>
    public int PoolSize { get; set; } = Environment.ProcessorCount;
}
