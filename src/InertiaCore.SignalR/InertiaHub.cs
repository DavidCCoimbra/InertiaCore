using Microsoft.AspNetCore.SignalR;

namespace InertiaCore.SignalR;

/// <summary>
/// SignalR hub for Inertia real-time prop updates.
/// Clients subscribe to components they're viewing; the server pushes refresh signals
/// when props change. The client then does a standard Inertia partial reload.
/// </summary>
public sealed class InertiaHub : Hub
{
    /// <summary>
    /// Subscribe to prop updates for a specific component.
    /// Called automatically by the client plugin when a page mounts.
    /// </summary>
    public async Task SubscribeToComponent(string component)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, component);
    }

    /// <summary>
    /// Unsubscribe from a component's prop updates.
    /// Called when the client navigates away from a page.
    /// </summary>
    public async Task UnsubscribeFromComponent(string component)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, component);
    }
}
