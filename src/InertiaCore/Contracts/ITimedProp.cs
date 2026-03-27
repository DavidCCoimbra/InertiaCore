using InertiaCore.Props.Behaviors;

namespace InertiaCore.Contracts;

/// <summary>
/// Indicates that a prop has a server-driven refresh interval.
/// The client polls for fresh data at the specified interval.
/// </summary>
public interface ITimedProp
{
    /// <summary>
    /// The timed refresh configuration for this prop.
    /// </summary>
    TimedBehavior Timed { get; }
}
