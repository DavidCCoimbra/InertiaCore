using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace InertiaCore.Razor;

/// <summary>
/// Tag helper for <c>&lt;inertia-head /&gt;</c> that renders SSR head content.
/// </summary>
[HtmlTargetElement("inertia-head", TagStructure = TagStructure.WithoutEndTag)]
public class InertiaHeadTagHelper : TagHelper
{
    /// <summary>
    /// The current view context, injected by the Razor engine.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; init; } = null!;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;

        if (ViewContext.ViewData["InertiaHead"] is string headContent)
        {
            output.Content.SetHtmlContent(headContent);
        }
    }
}
