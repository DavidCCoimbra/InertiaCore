using Microsoft.AspNetCore.SignalR;

namespace InertiaCore.SignalR;

/// <summary>
/// Broadcasts prop refresh signals to Inertia clients via SignalR.
/// The signal tells the client WHICH props to reload — the actual data
/// is fetched via a standard Inertia partial reload request.
/// </summary>
public sealed class InertiaBroadcaster(IHubContext<InertiaHub> hub) : IInertiaBroadcaster
{
    /// <inheritdoc />
    public async Task RefreshProps(string component, string[] only)
    {
        await hub.Clients.Group(component).SendAsync("inertia:reload", new
        {
            component,
            only,
        });
    }

    /// <inheritdoc />
    public async Task RefreshPropsForUser(string userId, string component, string[] only)
    {
        await hub.Clients.User(userId).SendAsync("inertia:reload", new
        {
            component,
            only,
        });
    }

    /// <inheritdoc />
    public async Task RefreshAll(string component)
    {
        await hub.Clients.Group(component).SendAsync("inertia:reload", new
        {
            component,
        });
    }

    /// <inheritdoc />
    public async Task RefreshAllForUser(string userId, string component)
    {
        await hub.Clients.User(userId).SendAsync("inertia:reload", new
        {
            component,
        });
    }
}
