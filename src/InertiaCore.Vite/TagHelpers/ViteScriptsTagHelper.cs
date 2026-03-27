using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using InertiaCore.Vite.Constants;
using InertiaCore.Vite.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace InertiaCore.Vite.TagHelpers;

/// <summary>
/// Razor Tag Helper that emits Vite script and stylesheet tags.
/// </summary>
[HtmlTargetElement("vite-scripts", TagStructure = TagStructure.WithoutEndTag)]
public class ViteScriptsTagHelper(IViteAssetResolver resolver) : TagHelper
{
    /// <summary>
    /// Optional entry points to include. Falls back to the configured defaults.
    /// </summary>
    [HtmlAttributeName("entryPoints")]
    public string[]? EntryPoints { get; init; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;

        var entryPoints = EntryPoints ?? resolver.DefaultEntryPoints;

        if (resolver.IsDevServerRunning())
        {
            RenderDevMode(output, entryPoints);
            return;
        }

        RenderProductionMode(output, entryPoints);
    }

    private void RenderDevMode(TagHelperOutput output, string[] entryPoints)
    {
        var devUrl = resolver.GetDevServerUrl();

        output.Content.AppendHtml(BuildScript($"{devUrl}/@vite/client"));

        if (resolver.Options.ReactRefresh)
        {
            output.Content.AppendHtml(BuildReactRefreshPreamble(devUrl));
        }

        foreach (var entry in entryPoints)
        {
            output.Content.AppendHtml(BuildScript($"{devUrl}/{entry}"));
        }
    }

    private void RenderProductionMode(TagHelperOutput output, string[] entryPoints)
    {
        foreach (var entry in entryPoints)
        {
            var assets = resolver.ResolveEntrypoint(entry);

            foreach (var preload in assets.PreloadFiles)
            {
                output.Content.AppendHtml(BuildModulePreload($"/{preload}"));
            }

            foreach (var css in assets.CssFiles)
            {
                output.Content.AppendHtml(BuildStylesheet($"/{css}"));
            }

            output.Content.AppendHtml(BuildScript($"/{assets.JsFile}"));
        }
    }

    private static string BuildScript(string src)
    {
        var tag = new TagBuilder("script");
        tag.Attributes["type"] = "module";
        tag.Attributes["src"] = src;

        using var writer = new StringWriter();
        tag.TagRenderMode = TagRenderMode.Normal;
        tag.WriteTo(writer, HtmlEncoder.Default);
        writer.Write('\n');
        return writer.ToString();
    }

    private static string BuildModulePreload(string href)
    {
        var tag = new TagBuilder("link");
        tag.Attributes["rel"] = "modulepreload";
        tag.Attributes["href"] = href;

        using var writer = new StringWriter();
        tag.TagRenderMode = TagRenderMode.SelfClosing;
        tag.WriteTo(writer, HtmlEncoder.Default);
        writer.Write('\n');
        return writer.ToString();
    }

    private static string BuildStylesheet(string href)
    {
        var tag = new TagBuilder("link");
        tag.Attributes["rel"] = "stylesheet";
        tag.Attributes["href"] = href;

        using var writer = new StringWriter();
        tag.TagRenderMode = TagRenderMode.SelfClosing;
        tag.WriteTo(writer, HtmlEncoder.Default);
        writer.Write('\n');
        return writer.ToString();
    }

    private static readonly CompositeFormat s_reactRefreshFormat =
        CompositeFormat.Parse(ViteScripts.ReactRefreshPreamble);

    private static string BuildReactRefreshPreamble(string devUrl)
    {
        var encodedUrl = HtmlEncoder.Default.Encode($"{devUrl}/@react-refresh");
        var body = string.Format(CultureInfo.InvariantCulture, s_reactRefreshFormat, encodedUrl);

        var tag = new TagBuilder("script");
        tag.Attributes["type"] = "module";
        tag.InnerHtml.AppendHtml($"\n{body}\n");

        using var writer = new StringWriter();
        tag.TagRenderMode = TagRenderMode.Normal;
        tag.WriteTo(writer, HtmlEncoder.Default);
        writer.Write('\n');
        return writer.ToString();
    }
}
