namespace InertiaCore.Configuration;

/// <summary>
/// Configuration options for server-side rendering.
/// </summary>
public class SsrOptions
{
    /// <summary>
    /// Enable or disable server-side rendering.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// URL of the Node.js SSR sidecar.
    /// </summary>
    public string Url { get; init; } = "http://127.0.0.1:13714";

    /// <summary>
    /// Whether to throw an exception when SSR fails. When false, falls back to client-side rendering.
    /// </summary>
    public bool ThrowOnError { get; set; }

    /// <summary>
    /// Timeout for SSR requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;
}
