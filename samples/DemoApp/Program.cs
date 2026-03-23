using InertiaCore.Core;
using InertiaCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInertia(options =>
{
    options.Version = "1.0.0";
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseInertia();

app.MapGet("/", (InertiaResponseFactory inertia) =>
    inertia.Render("Home/Index", new { Greeting = "Hello from Inertia!" }));

app.MapGet("/api/health", () => Results.Ok(new { Status = "ok" }));

app.MapMethods("/redirect", new[] { "PUT" }, () => Results.Redirect("/"));

app.Run();

/// <summary>
/// Entry point marker for WebApplicationFactory in integration tests.
/// </summary>
public partial class Program;
