using InertiaCore;
using InertiaCore.Attributes;
using InertiaCore.Core;
using InertiaCore.SignalR;
using Microsoft.AspNetCore.Mvc;

namespace Sandbox.Controllers;

public sealed class FeaturesController(IInertiaResponseFactory inertia) : Controller
{
    private static int s_counter;

    // --- Props showcase ---

    [HttpGet("/features/props")]
    public IActionResult Props()
    {
        return inertia.Render("features/Props", new PropsShowcase
        {
            ServerTime = DateTime.UtcNow.ToString("HH:mm:ss"),
            HeavyData = "This was loaded via deferred partial reload!",
            Analytics = "Analytics data loaded in the 'analytics' group",
            Items = ["apple", "banana", "cherry"],
            Permissions = ["read", "write", "admin"],
            SecretData = "You explicitly requested this optional prop",
            IsAdmin = true,
            AdminPanel = "Welcome to the admin panel!",
            Stats = "Stats loaded with fallback support",
            LiveClock = DateTime.UtcNow.ToString("HH:mm:ss.fff"),
        });
    }

    // --- Flash & Validation ---

    [HttpGet("/features/flash")]
    public IActionResult Flash()
    {
        return inertia.Render("features/Flash");
    }

    [HttpPost("/features/flash/success")]
    public IResult FlashSuccess([FromForm] string message)
    {
        return inertia.Back()
            .WithFlash("success", string.IsNullOrEmpty(message) ? "Action completed!" : message);
    }

    [HttpPost("/features/flash/error")]
    public IResult FlashError()
    {
        return inertia.Back()
            .WithErrors(new Dictionary<string, string>
            {
                ["email"] = "The email field is required.",
                ["name"] = "Name must be at least 3 characters.",
            });
    }

    [HttpPost("/features/flash/error-bag")]
    public IResult FlashErrorBag()
    {
        return inertia.Back()
            .WithErrors(new Dictionary<string, string>
            {
                ["username"] = "Username is already taken.",
            }, errorBag: "register");
    }

    // --- Live props (SignalR) ---

    private static readonly List<string> s_feed = ["System started."];

    [HttpGet("/features/live")]
    public IActionResult Live()
    {
        return inertia.Render("features/Live", new
        {
            Counter = (object)Inertia.Always(s_counter),
            Timestamp = (object)Inertia.Always(DateTime.UtcNow.ToString("HH:mm:ss")).RefreshEvery(TimeSpan.FromSeconds(10)),
            Feed = (object)Inertia.Always(s_feed.TakeLast(10).ToArray()),
            ServerStatus = (object)Inertia.Always("running"),
            // Cross-page channel: same channel as /features/listing
            CurrentBid = (object)Inertia.Always(s_currentBid).WithLive("listing:mustang"),
            BidCount = (object)Inertia.Always(s_bidCount).WithLive("listing:mustang"),
        });
    }

    // Direct action: user clicks button → push to all tabs instantly via WS
    [HttpPost("/features/live/increment")]
    public async Task<IResult> Increment([FromServices] IInertiaBroadcaster? broadcaster)
    {
        var newValue = Interlocked.Increment(ref s_counter);

        if (broadcaster != null)
        {
            await broadcaster.PushProps("features/Live", new { counter = newValue });
        }

        return Results.Ok(new { counter = newValue });
    }

    // Indirect: simulate a background event (e.g., webhook, job, external system)
    // The user doesn't trigger this — the server pushes data on its own
    [HttpPost("/features/live/simulate-event")]
    public async Task<IResult> SimulateEvent([FromServices] IInertiaBroadcaster? broadcaster)
    {
        // Simulate a background event: new data arrives from an external system
        var message = $"[{DateTime.UtcNow:HH:mm:ss}] External event received: order #{Random.Shared.Next(1000, 9999)} processed";
        s_feed.Add(message);

        if (broadcaster != null)
        {
            // Push the new feed directly over WebSocket — no page reload needed
            await broadcaster.PushProps("features/Live", new
            {
                feed = s_feed.TakeLast(10).ToArray(),
                serverStatus = "event received",
            });
        }

        return Results.Ok(new { status = "event simulated" });
    }


    // --- Channel-based live props (cross-page) ---

    private static decimal s_currentBid = 1000m;
    private static int s_bidCount;

    // Page A: shows the listing with a live bid
    [HttpGet("/features/listing")]
    public IActionResult Listing()
    {
        return inertia.Render("features/Listing", new
        {
            ListingTitle = "1967 Ford Mustang Fastback",
            CurrentBid = Inertia.Always(s_currentBid).WithLive("listing:mustang"),
            BidCount = Inertia.Always(s_bidCount).WithLive("listing:mustang"),
        });
    }

    // The Live page also shows the bid — proves cross-page channel updates
    // (LiveShowcase already has Counter, Timestamp, Feed — we add bid display in the Vue component)

    [HttpPost("/features/listing/bid")]
    public async Task<IResult> PlaceBid([FromServices] IInertiaBroadcaster? broadcaster)
    {
        var increment = Random.Shared.Next(50, 500);
        s_currentBid += increment;
        Interlocked.Increment(ref s_bidCount);

        if (broadcaster != null)
        {
            // PushToChannel: updates EVERY page that has a prop on "listing:mustang"
            // Both /features/listing AND /features/live will update
            await broadcaster.PushToChannel("listing:mustang", new
            {
                currentBid = s_currentBid,
                bidCount = s_bidCount,
            });
        }

        return Results.Ok(new { currentBid = s_currentBid, bidCount = s_bidCount });
    }

    // --- Async page data ---

    [HttpGet("/features/async")]
    public IActionResult Async()
    {
        return inertia.Render("features/Async", new AsyncShowcase
        {
            Title = "Async Page Data Demo",
            SmallProp = "I'm in the HTML (shared/pageData)",
            HeavyPayload = Enumerable.Range(1, 100)
                .Select(i => $"Item {i}: {Guid.NewGuid()}")
                .ToArray(),
        });
    }

    [HttpGet("/features/async-inline")]
    [InertiaInlinePageData]
    public IActionResult AsyncInline()
    {
        return inertia.Render("features/Async", new AsyncShowcase
        {
            Title = "Inline Page Data (no async fetch)",
            SmallProp = "Everything is in the HTML",
            HeavyPayload = Enumerable.Range(1, 100)
                .Select(i => $"Item {i}: {Guid.NewGuid()}")
                .ToArray(),
        });
    }
}

// --- Typed Props ---

public sealed class PropsShowcase
{
    [InertiaAlways]
    public required string ServerTime { get; init; }

    [InertiaDefer]
    public required string HeavyData { get; init; }

    [InertiaDefer(Group = "analytics")]
    public required string Analytics { get; init; }

    [InertiaMerge]
    public required string[] Items { get; init; }

    [InertiaOnce]
    public required string[] Permissions { get; init; }

    [InertiaOptional]
    public required string SecretData { get; init; }

    public required bool IsAdmin { get; init; }

    [InertiaWhen(nameof(IsAdmin))]
    public required string AdminPanel { get; init; }

    [InertiaDefer]
    [InertiaFallback(typeof(FallbackStats))]
    public required string Stats { get; init; }

    [InertiaAlways]
    [InertiaTimed(IntervalSeconds = 5)]
    public required string LiveClock { get; init; }
}

public sealed class FallbackStats
{
    public string Message { get; init; } = "Loading stats...";
}

public sealed class LiveShowcase
{
    [InertiaAlways]
    [InertiaLive(Channel = "counter")]
    public required int Counter { get; init; }

    [InertiaAlways]
    [InertiaTimed(IntervalSeconds = 10)]
    public required string Timestamp { get; init; }

    [InertiaAlways]
    [InertiaLive(Channel = "feed")]
    public required string[] Feed { get; init; }

    [InertiaAlways]
    public required string ServerStatus { get; init; }
}

public sealed class AsyncShowcase
{
    [InertiaPageData]
    [InertiaAlways]
    public required string Title { get; init; }

    [InertiaPageData]
    [InertiaAlways]
    public required string SmallProp { get; init; }

    public required string[] HeavyPayload { get; init; }
}
