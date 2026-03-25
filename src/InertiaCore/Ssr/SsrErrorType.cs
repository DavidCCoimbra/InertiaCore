namespace InertiaCore.Ssr;

/// <summary>
/// Types of errors that can occur during server-side rendering.
/// </summary>
public enum SsrErrorType
{
    /// <summary>
    /// The SSR sidecar refused the connection.
    /// </summary>
    ConnectionRefused,

    /// <summary>
    /// The SSR request timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// The SSR sidecar returned an invalid or unparseable response.
    /// </summary>
    InvalidResponse,

    /// <summary>
    /// The SSR sidecar returned an error during rendering.
    /// </summary>
    RenderError,
}
