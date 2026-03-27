using InertiaCore.Props.Behaviors;

namespace InertiaCore.Contracts;

/// <summary>
/// Indicates that a prop has a fallback value used when resolution fails.
/// </summary>
public interface IFallbackProp
{
    /// <summary>
    /// The fallback configuration for this prop.
    /// </summary>
    FallbackBehavior Fallback { get; }
}
