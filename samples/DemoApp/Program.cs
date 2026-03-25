using InertiaCore.Core;
using InertiaCore.Extensions;
#if HAS_VITE
using InertiaCore.Vite.Extensions;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInertia(options =>
{
    options.Version = "1.0.0";
});
#if HAS_VITE
builder.Services.AddVite(options =>
{
    options.EntryPoints = ["ClientApp/app.ts"];
});
#endif
builder.Services.AddControllersWithViews()
    .AddCookieTempDataProvider();

var app = builder.Build();

app.UseStaticFiles();
app.UseInertia();

// Simple page — anonymous object props
app.MapGet("/", (InertiaResponseFactory inertia) =>
    inertia.Render("Home/Index", new { Greeting = "Hello from Inertia!" }));

// All prop types — dictionary props
app.MapGet("/dashboard", (InertiaResponseFactory inertia) =>
    inertia.Render("Dashboard/Index", new Dictionary<string, object?>
    {
        ["user"] = InertiaResponseFactory.Always("Alice"),
        ["stats"] = InertiaResponseFactory.Defer(() => (object?)"heavy-stats", group: "analytics"),
        ["items"] = InertiaResponseFactory.Merge(new[] { 1, 2, 3 }),
        ["lazy"] = InertiaResponseFactory.Optional(() => (object?)"optional-data"),
        ["permissions"] = InertiaResponseFactory.Once(() => (object?)new[] { "read", "write", "admin" }),
    }));

// Shared props — demonstrate Share + ShareOnce
app.MapGet("/shared", (InertiaResponseFactory inertia) =>
{
    inertia.Share("appName", "InertiaCore DemoApp");
    inertia.Share("timestamp", DateTimeOffset.UtcNow.ToString("o"));
    inertia.ShareOnce("serverInfo", () => (object?)new { Runtime = "net10.0", Pid = Environment.ProcessId });
    return inertia.Render("Shared/Index", new { PageTitle = "Shared Props Demo" });
});

// Merge strategies — deep merge, append, prepend
app.MapGet("/merge", (InertiaResponseFactory inertia) =>
    inertia.Render("Merge/Index", new Dictionary<string, object?>
    {
        ["appendList"] = InertiaResponseFactory.Merge(new[] { "item-1", "item-2", "item-3" }),
        ["deepConfig"] = InertiaResponseFactory.Merge(new { Theme = "dark", Lang = "en" }).WithDeepMerge(),
    }));

// Flash data — form submit with redirect
app.MapGet("/flash", (InertiaResponseFactory inertia) =>
    inertia.Render("Flash/Index"));

app.MapPost("/flash", (InertiaResponseFactory inertia) =>
{
    inertia.Flash("success", "Form submitted successfully!");
    inertia.Flash("timestamp", DateTimeOffset.UtcNow.ToString("T"));
    return Results.Redirect("/flash");
});

app.MapGet("/api/health", () => Results.Ok(new { Status = "ok" }));

app.MapMethods("/redirect", new[] { "PUT" }, () => Results.Redirect("/"));

app.Run();

/// <summary>
/// Entry point marker for WebApplicationFactory in integration tests.
/// </summary>
public partial class Program;
