using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Models;
using InertiaCore.Vite.Services;
using InertiaCore.Vite.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NSubstitute;

namespace InertiaCore.Vite.Tests;

[Trait("Class", "ViteScriptsTagHelper")]
public class ViteScriptsTagHelperTests
{
    // -- Dev mode --

    [Fact]
    public void Dev_mode_emits_vite_client_script()
    {
        var resolver = CreateDevResolver("http://localhost:5173");
        var output = ProcessTagHelper(resolver);

        Assert.Contains("/@vite/client", output);
        Assert.Contains("type=\"module\"", output);
    }

    [Fact]
    public void Dev_mode_emits_entry_point_scripts()
    {
        var resolver = CreateDevResolver("http://localhost:5173", entryPoints: ["app.ts"]);
        var output = ProcessTagHelper(resolver);

        Assert.Contains("http://localhost:5173/app.ts", output);
    }

    [Fact]
    public void Dev_mode_emits_multiple_entry_points()
    {
        var resolver = CreateDevResolver("http://localhost:5173", entryPoints: ["app.ts", "admin.ts"]);
        var output = ProcessTagHelper(resolver);

        Assert.Contains("http://localhost:5173/app.ts", output);
        Assert.Contains("http://localhost:5173/admin.ts", output);
    }

    [Fact]
    public void Dev_mode_emits_react_refresh_preamble_when_enabled()
    {
        var resolver = CreateDevResolver("http://localhost:5173", reactRefresh: true);
        var output = ProcessTagHelper(resolver);

        Assert.Contains("@react-refresh", output);
    }

    [Fact]
    public void Dev_mode_skips_react_refresh_when_disabled()
    {
        var resolver = CreateDevResolver("http://localhost:5173", reactRefresh: false);
        var output = ProcessTagHelper(resolver);

        Assert.DoesNotContain("@react-refresh", output);
    }

    // -- Production mode --

    [Fact]
    public void Prod_mode_emits_js_script_from_manifest()
    {
        var resolver = CreateProdResolver(
            new ResolvedAssets("build/assets/app-abc123.js", [], []));
        var output = ProcessTagHelper(resolver);

        Assert.Contains("/build/assets/app-abc123.js", output);
        Assert.Contains("type=\"module\"", output);
    }

    [Fact]
    public void Prod_mode_emits_css_links_from_manifest()
    {
        var resolver = CreateProdResolver(
            new ResolvedAssets("build/app.js", ["build/assets/app-def456.css"], []));
        var output = ProcessTagHelper(resolver);

        Assert.Contains("/build/assets/app-def456.css", output);
        Assert.Contains("rel=\"stylesheet\"", output);
    }

    [Fact]
    public void Prod_mode_emits_multiple_css_files()
    {
        var resolver = CreateProdResolver(
            new ResolvedAssets("build/app.js", ["build/a.css", "build/b.css"], []));
        var output = ProcessTagHelper(resolver);

        Assert.Contains("/build/a.css", output);
        Assert.Contains("/build/b.css", output);
    }

    [Fact]
    public void Prod_mode_css_appears_before_js()
    {
        var resolver = CreateProdResolver(
            new ResolvedAssets("build/app.js", ["build/style.css"], []));
        var output = ProcessTagHelper(resolver);

        var cssIdx = output.IndexOf("style.css", StringComparison.Ordinal);
        var jsIdx = output.IndexOf("app.js", StringComparison.Ordinal);

        Assert.True(cssIdx < jsIdx, "CSS should appear before JS");
    }

    // -- Preload --

    [Fact]
    public void Prod_mode_emits_modulepreload_links()
    {
        var resolver = CreateProdResolver(
            new ResolvedAssets("build/app.js", [], ["build/assets/vendor-xyz.js"]));
        var output = ProcessTagHelper(resolver);

        Assert.Contains("modulepreload", output);
        Assert.Contains("/build/assets/vendor-xyz.js", output);
    }

    [Fact]
    public void Prod_mode_preloads_appear_before_css_and_js()
    {
        var resolver = CreateProdResolver(
            new ResolvedAssets("build/app.js", ["build/style.css"], ["build/vendor.js"]));
        var output = ProcessTagHelper(resolver);

        var preloadIdx = output.IndexOf("modulepreload", StringComparison.Ordinal);
        var cssIdx = output.IndexOf("style.css", StringComparison.Ordinal);
        var jsIdx = output.IndexOf("app.js", StringComparison.Ordinal);

        Assert.True(preloadIdx < cssIdx, "Preload should appear before CSS");
        Assert.True(cssIdx < jsIdx, "CSS should appear before JS");
    }

    [Fact]
    public void Prod_mode_skips_preload_when_none()
    {
        var resolver = CreateProdResolver(
            new ResolvedAssets("build/app.js", [], []));
        var output = ProcessTagHelper(resolver);

        Assert.DoesNotContain("modulepreload", output);
    }

    // -- Entry points --

    [Fact]
    public void Uses_custom_entry_points_when_provided()
    {
        var resolver = CreateDevResolver("http://localhost:5173", entryPoints: ["default.ts"]);
        var output = ProcessTagHelper(resolver, entryPoints: ["custom.ts"]);

        Assert.Contains("http://localhost:5173/custom.ts", output);
        Assert.DoesNotContain("default.ts", output);
    }

    [Fact]
    public void Falls_back_to_default_entry_points()
    {
        var resolver = CreateDevResolver("http://localhost:5173", entryPoints: ["fallback.ts"]);
        var output = ProcessTagHelper(resolver, entryPoints: null);

        Assert.Contains("http://localhost:5173/fallback.ts", output);
    }

    // -- Tag output --

    [Fact]
    public void Removes_tag_name()
    {
        var resolver = CreateDevResolver("http://localhost:5173");
        var (_, tagOutput) = ProcessTagHelperRaw(resolver);

        Assert.Null(tagOutput.TagName);
    }

    // -- Helpers --

    private static IViteAssetResolver CreateDevResolver(
        string devUrl,
        string[]? entryPoints = null,
        bool reactRefresh = false)
    {
        var resolver = Substitute.For<IViteAssetResolver>();
        resolver.IsDevServerRunning().Returns(true);
        resolver.GetDevServerUrl().Returns(devUrl);
        resolver.DefaultEntryPoints.Returns(entryPoints ?? ["resources/js/app.ts"]);
        resolver.Options.Returns(new ViteOptions { ReactRefresh = reactRefresh });
        return resolver;
    }

    private static IViteAssetResolver CreateProdResolver(ResolvedAssets assets)
    {
        var resolver = Substitute.For<IViteAssetResolver>();
        resolver.IsDevServerRunning().Returns(false);
        resolver.DefaultEntryPoints.Returns(["resources/js/app.ts"]);
        resolver.ResolveEntrypoint(Arg.Any<string>()).Returns(assets);
        resolver.Options.Returns(new ViteOptions());
        return resolver;
    }

    private static string ProcessTagHelper(
        IViteAssetResolver resolver,
        string[]? entryPoints = null)
    {
        var (_, output) = ProcessTagHelperRaw(resolver, entryPoints);
        return output.Content.GetContent();
    }

    private static (ViteScriptsTagHelper helper, TagHelperOutput output) ProcessTagHelperRaw(
        IViteAssetResolver resolver,
        string[]? entryPoints = null)
    {
        var helper = new ViteScriptsTagHelper(resolver);
        if (entryPoints is not null)
        {
            // Use reflection to set the init-only property for testing
            typeof(ViteScriptsTagHelper).GetProperty("EntryPoints")!.SetValue(helper, entryPoints);
        }

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "test");

        var output = new TagHelperOutput(
            "vite-scripts",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });

        helper.Process(context, output);
        return (helper, output);
    }
}
