using InertiaCore.Razor;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

namespace InertiaCore.Tests.Razor;

[Trait("Class", "InertiaTagHelper")]
public class SsrTagHelperTests
{
    [Fact]
    public void Renders_ssr_body_when_available()
    {
        var page = new Dictionary<string, object?> { ["component"] = "Test" };
        var tagHelper = CreateTagHelper(page, ssrBody: "<h1>SSR Rendered</h1>");
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        var content = output.Content.GetContent();
        Assert.Contains("SSR Rendered", content);
        Assert.Contains("data-page=", content);
        Assert.Contains("<h1>", content);
    }

    [Fact]
    public void Falls_back_to_data_page_without_ssr()
    {
        var page = new Dictionary<string, object?> { ["component"] = "Test" };
        var tagHelper = CreateTagHelper(page, ssrBody: null);
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        var content = output.Content.GetContent();
        Assert.Contains("data-page=", content);
    }

    [Fact]
    public void Head_tag_helper_renders_ssr_head()
    {
        var headHelper = CreateHeadTagHelper(ssrHead: "<title>SSR Title</title>");
        var output = CreateOutput("inertia-head");

        headHelper.Process(CreateContext("inertia-head"), output);

        Assert.Contains("<title>SSR Title</title>", output.Content.GetContent());
    }

    [Fact]
    public void Head_tag_helper_empty_without_ssr()
    {
        var headHelper = CreateHeadTagHelper(ssrHead: null);
        var output = CreateOutput("inertia-head");

        headHelper.Process(CreateContext("inertia-head"), output);

        Assert.False(output.IsContentModified);
    }

    private static InertiaTagHelper CreateTagHelper(
        Dictionary<string, object?> page, string? ssrBody)
    {
        var viewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["Page"] = page,
        };

        if (ssrBody != null)
        {
            viewData["InertiaBody"] = ssrBody;
        }

        var viewContext = new ViewContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            Substitute.For<IView>(),
            viewData,
            Substitute.For<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());

        return new InertiaTagHelper { ViewContext = viewContext };
    }

    private static InertiaHeadTagHelper CreateHeadTagHelper(string? ssrHead)
    {
        var viewData = new ViewDataDictionary(
            new EmptyModelMetadataProvider(), new ModelStateDictionary());

        if (ssrHead != null)
        {
            viewData["InertiaHead"] = ssrHead;
        }

        var viewContext = new ViewContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            Substitute.For<IView>(),
            viewData,
            Substitute.For<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());

        return new InertiaHeadTagHelper { ViewContext = viewContext };
    }

    private static TagHelperContext CreateContext(string tagName = "inertia")
    {
        return new TagHelperContext(
            tagName,
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
    }

    private static TagHelperOutput CreateOutput(string tagName = "inertia")
    {
        return new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }
}
