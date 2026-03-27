using InertiaCore.Ssr;

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
    /// Transport protocol for SSR communication.
    /// </summary>
    public SsrTransport Transport { get; set; } = SsrTransport.Http;

    /// <summary>
    /// URL of the Node.js SSR sidecar (used with Http transport).
    /// </summary>
    public string Url { get; set; } = "http://127.0.0.1:13714";

    /// <summary>
    /// Unix Domain Socket path (used with MessagePack transport).
    /// </summary>
    public string SocketPath { get; set; } = "/tmp/inertia-ssr.sock";

    /// <summary>
    /// Whether to throw an exception when SSR fails. When false, falls back to client-side rendering.
    /// </summary>
    public bool ThrowOnError { get; set; }

    /// <summary>
    /// Timeout for SSR requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// Request paths excluded from SSR. Matching paths will always use client-side rendering.
    /// </summary>
    public string[] ExcludedPaths { get; set; } = [];
}
