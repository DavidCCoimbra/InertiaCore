using Microsoft.AspNetCore.SignalR;

namespace InertiaCore.SignalR;

/// <summary>
/// SignalR hub for Inertia real-time prop updates.
/// Clients subscribe to components and custom groups; the server pushes
/// prop updates directly or signals clients to reload via HTTP.
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

    /// <summary>
    /// Join a custom group (e.g., "team-engineering", "room-5", "user:123").
    /// </summary>
    public async Task JoinGroup(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    /// <summary>
    /// Leave a custom group.
    /// </summary>
    public async Task LeaveGroup(string group)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
}
