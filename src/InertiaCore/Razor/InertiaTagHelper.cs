using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using InertiaCore.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace InertiaCore.Razor;

/// <summary>
/// Tag helper for <c>&lt;inertia /&gt;</c> that renders the root div with the data-page attribute.
/// </summary>
[HtmlTargetElement("inertia", TagStructure = TagStructure.WithoutEndTag)]
public partial class InertiaTagHelper : TagHelper
{
    private static readonly JsonSerializerOptions s_jsonOptions = InertiaJsonOptions.CamelCase;

    /// <summary>
    /// The id attribute for the root div element.
    /// </summary>
    [HtmlAttributeName("id")]
    public string Id { get; set; } = "app";

    /// <summary>
    /// The current view context, injected by the Razor engine.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; init; } = null!;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var page = ViewContext.ViewData["Page"] as Dictionary<string, object?>;

        if (page == null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = null;

        // SSR: the sidecar returns the full div with data-page and rendered content
        if (ViewContext.ViewData["InertiaBody"] is string ssrBody)
        {
            // Async page data: strip the embedded <script data-page> and replace with
            // a minimal version + an inline fetch script for the full props
            if (ViewContext.ViewData["AsyncPageDataUrl"] is string asyncUrl)
            {
                var strippedBody = PageScriptPattern().Replace(ssrBody, "");

                var minimalPage = BuildMinimalPage(page);
                var minimalJson = JsonSerializer.Serialize(minimalPage, s_jsonOptions);

                var safeUrl = JsonSerializer.Serialize(asyncUrl, s_jsonOptions);

                output.Content.SetHtmlContent(
                    strippedBody +
                    $"<script data-page=\"{HtmlEncoder.Default.Encode(Id)}\" type=\"application/json\">{minimalJson}</script>" +
                    $"<script>window.__inertiaPageData=fetch({safeUrl}).then(r=>r.json())</script>");
                return;
            }

            output.Content.SetHtmlContent(ssrBody);
            return;
        }

        // CSR: script tag (Inertia v3) + div for client-side rendering
        var json = JsonSerializer.Serialize(page, s_jsonOptions);
        output.Content.SetHtmlContent(
            $"<script data-page=\"{HtmlEncoder.Default.Encode(Id)}\" type=\"application/json\">{json}</script>" +
            $"<div id=\"{HtmlEncoder.Default.Encode(Id)}\"></div>");
    }

    private static Dictionary<string, object?> BuildMinimalPage(Dictionary<string, object?> page)
    {
        var minimal = new Dictionary<string, object?>(page);

        // Keep shared props (errors, flash, auth) but remove component-specific props
        if (minimal.TryGetValue("props", out var propsObj)
            && propsObj is Dictionary<string, object?> props
            && minimal.TryGetValue("sharedProps", out var sharedObj)
            && sharedObj is IEnumerable<string> sharedKeys)
        {
            var pageDataKeys = minimal.TryGetValue("pageDataProps", out var pdObj)
                && pdObj is IEnumerable<string> pdKeys
                ? new HashSet<string>(pdKeys) : [];

            var shared = new HashSet<string>(sharedKeys);
            shared.UnionWith(pageDataKeys);

            var filteredProps = new Dictionary<string, object?>();
            foreach (var (key, value) in props)
            {
                if (shared.Contains(key))
                {
                    filteredProps[key] = value;
                }
            }

            minimal["props"] = filteredProps;
        }
        else
        {
            // No metadata about shared props — remove all props
            minimal.Remove("props");
        }

        // Remove internal metadata keys from the client-facing page object
        minimal.Remove("sharedProps");
        minimal.Remove("pageDataProps");

        return minimal;
    }

    [GeneratedRegex(@"<script data-page=""[^""]*"" type=""application/json"">.*?</script>")]
    private static partial Regex PageScriptPattern();
}
