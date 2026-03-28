using InertiaCore.EmbeddedV8;
using InertiaCore.Extensions;
using InertiaCore.Ssr;
using InertiaCore.Vite.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Read SSR profile from environment variable (set by Makefile) or appsettings fallback
var ssrMode = Environment.GetEnvironmentVariable("SSR_MODE")
    ?? builder.Configuration["SsrMode"]
    ?? "http";

var ssrEnabled = Environment.GetEnvironmentVariable("SSR_ENABLED") is { } ssrEnv
    ? ssrEnv != "false"
    : builder.Configuration.GetValue("SsrEnabled", true);

var asyncPageData = Environment.GetEnvironmentVariable("ASYNC_PAGE_DATA") is { } asyncEnv
    ? asyncEnv == "true"
    : builder.Configuration.GetValue("AsyncPageData", false);

builder.Services.AddControllersWithViews()
    .AddCookieTempDataProvider();

builder.Services.AddInertia(options =>
{
    options.Version = "1.0.0";
    options.Ssr.Enabled = ssrEnabled;
    options.Ssr.AsyncPageData = asyncPageData;

    if (ssrMode == "msgpack")
    {
        options.Ssr.Transport = SsrTransport.MessagePack;
    }
});

builder.Services.AddVite(options =>
{
    options.EntryPoints = ["ClientApp/app.ts"];
});

builder.Services.AddInertiaSharedProps(ctx => new
{
    AppName = "InertiaCore Sandbox",
    Year = DateTime.Now.Year,
});

// Register EmbeddedV8 only when in V8 mode
if (ssrMode == "v8")
{
    builder.Services.AddInertiaEmbeddedV8(options =>
    {
        options.BundlePath = "dist/ssr/ssr.js";
        options.PoolSize = 2; // Smaller pool for faster warmup during development
    });
}

var app = builder.Build();

app.UseStaticFiles();
app.UseInertia();

app.MapControllers();
app.MapInertiaPageData();
if (ssrMode == "v8")
{
    app.MapInertiaV8Reload();
}

app.Run();
