using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace InertiaCore.SignalR.Tests;

[Trait("Class", "InertiaBroadcaster")]
public class InertiaBroadcasterTests
{
    [Fact]
    public async Task RefreshProps_sends_to_component_group()
    {
        var hub = Substitute.For<IHubContext<InertiaHub>>();
        var clients = Substitute.For<IHubClients>();
        var groupProxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.Group("Dashboard/Index").Returns(groupProxy);

        var broadcaster = new InertiaBroadcaster(hub);

        await broadcaster.RefreshProps("Dashboard/Index", ["notifications", "stats"]);

        await groupProxy.Received(1).SendCoreAsync(
            "inertia:reload",
            Arg.Is<object?[]>(args => args.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshPropsForUser_sends_to_specific_user()
    {
        var hub = Substitute.For<IHubContext<InertiaHub>>();
        var clients = Substitute.For<IHubClients>();
        var userProxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.User("user-123").Returns(userProxy);

        var broadcaster = new InertiaBroadcaster(hub);

        await broadcaster.RefreshPropsForUser("user-123", "Dashboard/Index", ["notifications"]);

        await userProxy.Received(1).SendCoreAsync(
            "inertia:reload",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAll_sends_to_component_group()
    {
        var hub = Substitute.For<IHubContext<InertiaHub>>();
        var clients = Substitute.For<IHubClients>();
        var groupProxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.Group("Contacts/Index").Returns(groupProxy);

        var broadcaster = new InertiaBroadcaster(hub);

        await broadcaster.RefreshAll("Contacts/Index");

        await groupProxy.Received(1).SendCoreAsync(
            "inertia:reload",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAllForUser_sends_to_specific_user()
    {
        var hub = Substitute.For<IHubContext<InertiaHub>>();
        var clients = Substitute.For<IHubClients>();
        var userProxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.User("user-456").Returns(userProxy);

        var broadcaster = new InertiaBroadcaster(hub);

        await broadcaster.RefreshAllForUser("user-456", "Dashboard/Index");

        await userProxy.Received(1).SendCoreAsync(
            "inertia:reload",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }
}
