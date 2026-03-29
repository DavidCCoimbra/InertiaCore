namespace InertiaCore.Exceptions;

/// <summary>
/// Exception thrown when a referenced Inertia component cannot be found.
/// </summary>
public sealed class ComponentNotFoundException : InertiaException
{
    /// <summary>
    /// Initializes a new instance of <see cref="ComponentNotFoundException"/>.
    /// </summary>
    public ComponentNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified message.
    /// </summary>
    public ComponentNotFoundException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified message and inner exception.
    /// </summary>
    public ComponentNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
