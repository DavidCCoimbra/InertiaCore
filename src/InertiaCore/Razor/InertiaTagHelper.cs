using System.Text.Json;
using System.Web;
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
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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

        // If SSR body is available, render it instead of the data-page div
        if (ViewContext.ViewData["InertiaBody"] is string ssrBody)
        {
            output.Content.SetHtmlContent(ssrBody);
            return;
        }

        var json = JsonSerializer.Serialize(page, s_jsonOptions);
        var encoded = HttpUtility.HtmlAttributeEncode(json);
        output.Content.SetHtmlContent($"<div id=\"{Id}\" data-page=\"{encoded}\"></div>");
    }
}
