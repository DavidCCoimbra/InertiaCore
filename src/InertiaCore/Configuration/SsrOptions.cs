namespace InertiaCore.Configuration;

/// <summary>
/// Configuration options for server-side rendering.
/// </summary>
public class SsrOptions
{
    /// <summary>
    /// Enable or disable server-side rendering.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// URL of the Node.js SSR sidecar.
    /// </summary>
    public string Url { get; set; } = "http://127.0.0.1:13714";
}
