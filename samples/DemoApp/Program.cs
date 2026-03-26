using InertiaCore.Core;
using InertiaCore.Extensions;
#if HAS_VITE
using InertiaCore.Vite.Extensions;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInertia(options =>
{
    options.Version = "1.0.0";
    options.Ssr.Enabled = true;
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
app.MapGet("/", (IInertiaResponseFactory inertia) =>
    inertia.Render("Home/Index", new { Greeting = "Hello from Inertia!" }));

// All prop types — dictionary props
app.MapGet("/dashboard", (IInertiaResponseFactory inertia) =>
    inertia.Render("Dashboard/Index", new Dictionary<string, object?>
    {
        ["user"] = InertiaResponseFactory.Always("Alice"),
        ["stats"] = InertiaResponseFactory.Defer(() => (object?)"heavy-stats", group: "analytics"),
        ["items"] = InertiaResponseFactory.Merge(new[] { 1, 2, 3 }),
        ["lazy"] = InertiaResponseFactory.Optional(() => (object?)"optional-data"),
        ["permissions"] = InertiaResponseFactory.Once(() => (object?)new[] { "read", "write", "admin" }),
    }));

// Shared props — demonstrate Share + ShareOnce
app.MapGet("/shared", (IInertiaResponseFactory inertia) =>
{
    inertia.Share("appName", "InertiaCore DemoApp");
    inertia.Share("timestamp", DateTimeOffset.UtcNow.ToString("o"));
    inertia.ShareOnce("serverInfo", () => (object?)new { Runtime = "net10.0", Pid = Environment.ProcessId });
    return inertia.Render("Shared/Index", new { PageTitle = "Shared Props Demo" });
});

// Merge strategies — deep merge, append, prepend
app.MapGet("/merge", (IInertiaResponseFactory inertia) =>
    inertia.Render("Merge/Index", new Dictionary<string, object?>
    {
        ["appendList"] = InertiaResponseFactory.Merge(new[] { "item-1", "item-2", "item-3" }),
        ["deepConfig"] = InertiaResponseFactory.Merge(new { Theme = "dark", Lang = "en" }).WithDeepMerge(),
    }));

// Flash data — form submit with redirect
app.MapGet("/flash", (IInertiaResponseFactory inertia) =>
    inertia.Render("Flash/Index"));

app.MapPost("/flash", (IInertiaResponseFactory inertia) =>
{
    inertia.Flash("success", "Form submitted successfully!");
    inertia.Flash("timestamp", DateTimeOffset.UtcNow.ToString("T"));
    return Results.Redirect("/flash");
});

// Validation errors — simulated form validation
app.MapGet("/validation", (IInertiaResponseFactory inertia) =>
    inertia.Render("Validation/Index"));

app.MapPost("/validation", (HttpContext context, IInertiaResponseFactory inertia) =>
{
    // Simulate validation failure by storing errors in TempData
    var errors = new Dictionary<string, string>
    {
        ["name"] = "Name is required.",
        ["email"] = "Email must be a valid address.",
    };
    var tempDataFactory = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>();
    var tempData = tempDataFactory.GetTempData(context);
    tempData[InertiaCore.Constants.SessionKeys.Errors] = System.Text.Json.JsonSerializer.Serialize(errors);
    tempData.Save();
    return Results.Redirect("/validation");
});

// Scroll prop — infinite scroll / pagination demo
app.MapGet("/scroll", (IInertiaResponseFactory inertia, HttpContext context) =>
{
    var page = int.TryParse(context.Request.Query["page"], out var p) ? p : 1;
    var pageSize = 5;
    var totalItems = 23;
    var allItems = Enumerable.Range(1, totalItems).Select(i => $"Item {i}").ToArray();
    var pageItems = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
    var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

    var metadata = new SimpleScrollMetadata(
        PageName: "page",
        PreviousPage: page > 1 ? page - 1 : null,
        NextPage: page < totalPages ? page + 1 : null,
        CurrentPage: page);

    return inertia.Render("Scroll/Index", new Dictionary<string, object?>
    {
        ["items"] = InertiaResponseFactory.Scroll(pageItems, wrapper: "data", metadataProvider: metadata),
        ["totalPages"] = totalPages,
    });
});

app.MapGet("/api/health", () => Results.Ok(new { Status = "ok" }));

app.MapMethods("/redirect", new[] { "PUT" }, () => Results.Redirect("/"));

app.Run();

/// <summary>
/// Entry point marker for WebApplicationFactory in integration tests.
/// </summary>
public partial class Program;

/// <summary>
/// Simple scroll metadata implementation for the demo.
/// </summary>
internal record SimpleScrollMetadata(
    string PageName, object? PreviousPage, object? NextPage, object? CurrentPage)
    : InertiaCore.Contracts.IProvidesScrollMetadata
{
    public string GetPageName() => PageName;
    public object? GetPreviousPage() => PreviousPage;
    public object? GetNextPage() => NextPage;
    public object? GetCurrentPage() => CurrentPage;
}
