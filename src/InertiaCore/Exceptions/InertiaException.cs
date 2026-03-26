namespace InertiaCore.Exceptions;

/// <summary>
/// Base exception for Inertia-related errors.
/// </summary>
public class InertiaException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="InertiaException"/>.
    /// </summary>
    public InertiaException()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified message.
    /// </summary>
    public InertiaException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified message and inner exception.
    /// </summary>
    public InertiaException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
