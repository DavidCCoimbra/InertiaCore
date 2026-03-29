using System.Text.Json.Serialization;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Configuration;

/// <summary>
/// Configuration options for server-side rendering.
/// </summary>
public sealed class SsrOptions
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

    /// <summary>
    /// Moves hydration props to a separate HTTP endpoint, reducing HTML document size.
    /// Only effective when SSR is enabled.
    /// </summary>
    public bool AsyncPageData { get; set; }

    /// <summary>
    /// Time-to-live in seconds for cached async page data entries.
    /// </summary>
    public int AsyncPageDataTtlSeconds { get; set; } = 30;

    /// <summary>
    /// URL path prefix for the async page data endpoint.
    /// </summary>
    public string AsyncPageDataPath { get; set; } = "/inertia/page-data";

    /// <summary>
    /// Resolves the identity used to scope async page data cache entries.
    /// Each entry can only be retrieved by the same identity that created it.
    /// Defaults to <c>HttpContext.User.Identity?.Name</c>.
    /// </summary>
    [JsonIgnore]
    public Func<HttpContext, string?> ResolvePageDataIdentity { get; set; } =
        ctx => ctx.User.Identity?.Name;
}
