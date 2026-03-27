using System.Text.Json;
using System.Web;
using InertiaCore.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace InertiaCore.Razor;

/// <summary>
/// Tag helper for <c>&lt;inertia /&gt;</c> that renders the root div with the data-page attribute.
/// </summary>
[HtmlTargetElement("inertia", TagStructure = TagStructure.WithoutEndTag)]
public class InertiaTagHelper : TagHelper
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
            output.Content.SetHtmlContent(ssrBody);
            return;
        }

        // CSR: script tag (Inertia v3) + div for client-side rendering
        var json = JsonSerializer.Serialize(page, s_jsonOptions);
        var encoded = HttpUtility.HtmlEncode(json);
        output.Content.SetHtmlContent(
            $"<script data-page=\"{Id}\" type=\"application/json\">{json}</script>" +
            $"<div id=\"{Id}\"></div>");
    }
}
