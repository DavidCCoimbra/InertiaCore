using InertiaCore.Props.Behaviors;

namespace InertiaCore.Contracts;

/// <summary>
/// Indicates that a prop supports deferred loading by the client.
/// </summary>
public interface IDeferrable
{
    /// <summary>
    /// The deferral configuration for this prop.
    /// </summary>
    DeferBehavior Defer { get; }
}
