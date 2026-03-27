namespace InertiaCore.SignalR;

/// <summary>
/// Service for broadcasting prop refresh signals to connected Inertia clients.
/// </summary>
public interface IInertiaBroadcaster
{
    /// <summary>
    /// Signals all clients viewing the specified component to reload the given props.
    /// </summary>
    Task RefreshProps(string component, string[] only);

    /// <summary>
    /// Signals a specific user to reload props on the specified component.
    /// </summary>
    Task RefreshPropsForUser(string userId, string component, string[] only);

    /// <summary>
    /// Signals all clients viewing the specified component to reload all props.
    /// </summary>
    Task RefreshAll(string component);

    /// <summary>
    /// Signals a specific user to reload all props on the specified component.
    /// </summary>
    Task RefreshAllForUser(string userId, string component);
}
