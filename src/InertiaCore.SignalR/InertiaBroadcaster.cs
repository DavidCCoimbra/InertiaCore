using Microsoft.AspNetCore.SignalR;

namespace InertiaCore.SignalR;

/// <summary>
/// Broadcasts prop updates to Inertia clients via SignalR.
/// </summary>
public sealed class InertiaBroadcaster(IHubContext<InertiaHub> hub) : IInertiaBroadcaster
{
    /// <inheritdoc />
    public Task RefreshProps(string component, string[]? only = null, string? group = null)
    {
        var target = hub.Clients.Group(group ?? component);
        return target.SendAsync("inertia:reload", new { component, only });
    }

    /// <inheritdoc />
    public Task PushProps(string component, object props, string? group = null)
    {
        var target = hub.Clients.Group(group ?? component);
        return target.SendAsync("inertia:props", new { component, props });
    }

    /// <inheritdoc />
    public Task PushToChannel(string channel, object props)
    {
        return hub.Clients.Group(channel).SendAsync("inertia:channel", new { channel, props });
    }
}
