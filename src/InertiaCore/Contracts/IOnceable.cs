using InertiaCore.Props.Behaviors;

namespace InertiaCore.Contracts;

/// <summary>
/// Indicates that a prop is resolved once and excluded from subsequent requests.
/// </summary>
public interface IOnceable
{
    /// <summary>
    /// The once-resolution configuration for this prop.
    /// </summary>
    OnceBehavior Once { get; }
}
