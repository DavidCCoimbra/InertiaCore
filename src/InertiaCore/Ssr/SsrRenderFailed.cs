namespace InertiaCore.Ssr;

/// <summary>
/// Event raised when server-side rendering fails. Used for logging and diagnostic hooks.
/// </summary>
public class SsrRenderFailed
{
    /// <summary>
    /// The type of error that occurred.
    /// </summary>
    public required SsrErrorType ErrorType { get; init; }

    /// <summary>
    /// The error message from the SSR sidecar or the HTTP layer.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The underlying exception, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// The page object that was being rendered when the error occurred.
    /// </summary>
    public Dictionary<string, object?>? Page { get; init; }
}
