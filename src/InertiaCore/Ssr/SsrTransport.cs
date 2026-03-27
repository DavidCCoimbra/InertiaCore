namespace InertiaCore.Ssr;

/// <summary>
/// Transport protocol for communicating with the SSR render engine.
/// </summary>
public enum SsrTransport
{
    /// <summary>
    /// JSON over HTTP to a Node.js sidecar (default, compatible with all setups).
    /// </summary>
    Http,

    /// <summary>
    /// MessagePack binary over Unix Domain Sockets (~4x faster IPC, ~40% smaller payloads).
    /// </summary>
    MessagePack,

    /// <summary>
    /// Embedded V8 engine in-process via ClearScript (zero serialization, zero network).
    /// </summary>
    EmbeddedV8,
}
