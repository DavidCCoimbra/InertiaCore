namespace InertiaCore.Ssr;

/// <summary>
/// Exception thrown when SSR fails and ThrowOnError is enabled in SsrOptions.
/// </summary>
public sealed class SsrException : Exception
{
    /// <summary>
    /// The type of SSR error that occurred.
    /// </summary>
    public SsrErrorType ErrorType { get; }

    /// <summary>
    /// Initializes a new SSR exception with the specified error type and message.
    /// </summary>
    public SsrException(SsrErrorType errorType, string message)
        : base(message)
    {
        ErrorType = errorType;
    }

    /// <summary>
    /// Initializes a new SSR exception with the specified error type, message, and inner exception.
    /// </summary>
    public SsrException(SsrErrorType errorType, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorType = errorType;
    }
}
