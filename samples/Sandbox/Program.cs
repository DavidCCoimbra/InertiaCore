using InertiaCore.Extensions;
using InertiaCore.Vite.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddCookieTempDataProvider();

builder.Services.AddInertia(options =>
{
    options.Version = "1.0.0";
    options.Ssr.Enabled = true;
    options.Ssr.AsyncPageData = true;
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

var app = builder.Build();

app.UseStaticFiles();
app.UseInertia();

app.MapControllers();
app.MapInertiaPageData();

app.Run();
