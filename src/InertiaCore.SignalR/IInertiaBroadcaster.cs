namespace InertiaCore.SignalR;

/// <summary>
/// Service for broadcasting prop updates to connected Inertia clients via SignalR.
/// </summary>
public interface IInertiaBroadcaster
{
    /// <summary>
    /// Signals clients to reload props via HTTP.
    /// When <paramref name="only"/> is null, all props are refreshed.
    /// When <paramref name="group"/> is null, targets all clients viewing the component.
    /// </summary>
    Task RefreshProps(string component, string[]? only = null, string? group = null);

    /// <summary>
    /// Pushes prop values directly to clients viewing a component via WebSocket.
    /// When <paramref name="group"/> is null, targets all clients viewing the component.
    /// </summary>
    Task PushProps(string component, object props, string? group = null);

    /// <summary>
    /// Pushes prop values to all clients subscribed to a channel via WebSocket.
    /// Works across pages — any prop with .WithLive(channel) receives the update
    /// regardless of which component the client is viewing.
    /// </summary>
    Task PushToChannel(string channel, object props);
}
