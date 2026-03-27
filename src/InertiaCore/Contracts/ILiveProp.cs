using InertiaCore.Props.Behaviors;

namespace InertiaCore.Contracts;

/// <summary>
/// Indicates that a prop receives real-time updates via SignalR.
/// When the channel fires, the client auto-reloads this prop.
/// </summary>
public interface ILiveProp
{
    /// <summary>
    /// The live update configuration for this prop.
    /// </summary>
    LiveBehavior Live { get; }
}
