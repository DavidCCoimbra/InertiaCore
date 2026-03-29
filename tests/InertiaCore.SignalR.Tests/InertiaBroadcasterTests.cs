using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace InertiaCore.SignalR.Tests;

[Trait("Class", "InertiaBroadcaster")]
public class InertiaBroadcasterTests
{
    // --- RefreshProps ---

    [Fact]
    public async Task RefreshProps_sends_to_component_group()
    {
        var (broadcaster, groupProxy) = CreateBroadcaster("Dashboard/Index");

        await broadcaster.RefreshProps("Dashboard/Index", ["notifications", "stats"]);

        await groupProxy.Received(1).SendCoreAsync(
            "inertia:reload",
            Arg.Is<object?[]>(args => args.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshProps_with_group_sends_to_specific_group()
    {
        var (broadcaster, groupProxy) = CreateBroadcaster("team-engineering");

        await broadcaster.RefreshProps("Dashboard/Index", ["notifications"], group: "team-engineering");

        await groupProxy.Received(1).SendCoreAsync(
            "inertia:reload",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshProps_without_only_refreshes_all()
    {
        var (broadcaster, groupProxy) = CreateBroadcaster("Dashboard/Index");

        await broadcaster.RefreshProps("Dashboard/Index");

        await groupProxy.Received(1).SendCoreAsync(
            "inertia:reload",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    // --- PushProps ---

    [Fact]
    public async Task PushProps_sends_to_component_group()
    {
        var (broadcaster, groupProxy) = CreateBroadcaster("features/Live");

        await broadcaster.PushProps("features/Live", new { counter = 42 });

        await groupProxy.Received(1).SendCoreAsync(
            "inertia:props",
            Arg.Is<object?[]>(args => args.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushProps_with_group_sends_to_specific_group()
    {
        var (broadcaster, groupProxy) = CreateBroadcaster("room-5");

        await broadcaster.PushProps("Auction/Show", new { currentBid = 5200 }, group: "room-5");

        await groupProxy.Received(1).SendCoreAsync(
            "inertia:props",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    // --- PushToChannel ---

    [Fact]
    public async Task PushToChannel_sends_to_channel_group()
    {
        var (broadcaster, groupProxy) = CreateBroadcaster("listing:123");

        await broadcaster.PushToChannel("listing:123", new { currentBid = 5200 });

        await groupProxy.Received(1).SendCoreAsync(
            "inertia:channel",
            Arg.Is<object?[]>(args => args.Length == 1),
            Arg.Any<CancellationToken>());
    }

    // --- Helpers ---

    private static (InertiaBroadcaster broadcaster, IClientProxy groupProxy) CreateBroadcaster(string expectedGroup)
    {
        var hub = Substitute.For<IHubContext<InertiaHub>>();
        var clients = Substitute.For<IHubClients>();
        var groupProxy = Substitute.For<IClientProxy>();
        hub.Clients.Returns(clients);
        clients.Group(expectedGroup).Returns(groupProxy);

        return (new InertiaBroadcaster(hub), groupProxy);
    }
}
