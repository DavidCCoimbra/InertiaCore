using InertiaCore.Props.Behaviors;

namespace InertiaCore.Contracts;

/// <summary>
/// Indicates that a prop's data is merged with existing client-side state.
/// </summary>
public interface IMergeable
{
    /// <summary>
    /// The merge configuration for this prop.
    /// </summary>
    MergeBehavior Merge { get; }
}
