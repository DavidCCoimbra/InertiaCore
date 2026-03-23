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
public class InertiaTagHelperTests
{
    [Fact]
    public void Renders_div_with_data_page_attribute()
    {
        var page = new Dictionary<string, object?>
        {
            ["component"] = "Home/Index",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/",
            ["version"] = "v1",
        };
        var tagHelper = CreateTagHelper(page);
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        var content = output.Content.GetContent();
        Assert.Contains("id=\"app\"", content);
        Assert.Contains("data-page=\"", content);
        Assert.Contains("Home/Index", content);
    }

    [Fact]
    public void Uses_custom_id()
    {
        var page = new Dictionary<string, object?> { ["component"] = "Test" };
        var tagHelper = CreateTagHelper(page);
        tagHelper.Id = "my-app";
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        Assert.Contains("id=\"my-app\"", output.Content.GetContent());
    }

    [Fact]
    public void Html_encodes_page_data_to_prevent_xss()
    {
        var page = new Dictionary<string, object?>
        {
            ["component"] = "Test",
            ["props"] = new Dictionary<string, object?> { ["name"] = "<script>alert('xss')</script>" },
        };
        var tagHelper = CreateTagHelper(page);
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        var content = output.Content.GetContent();
        // JSON serializer escapes < as \u003c, then HtmlAttributeEncode encodes further
        // Either way, raw <script> must never appear in the output
        Assert.DoesNotContain("<script>", content);
        Assert.Contains("data-page=", content);
    }

    [Fact]
    public void Suppresses_output_when_page_data_is_missing()
    {
        var tagHelper = CreateTagHelper(page: null);
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        Assert.True(output.IsContentModified is false || output.Content.GetContent() == "");
    }

    [Fact]
    public void Removes_inertia_tag_name()
    {
        var page = new Dictionary<string, object?> { ["component"] = "Test" };
        var tagHelper = CreateTagHelper(page);
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public void Serializes_with_camel_case()
    {
        var page = new Dictionary<string, object?>
        {
            ["component"] = "Test",
            ["props"] = new Dictionary<string, object?>(),
            ["url"] = "/",
            ["version"] = null,
        };
        var tagHelper = CreateTagHelper(page);
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        var content = output.Content.GetContent();
        // JSON keys are HTML-encoded: "component" becomes &quot;component&quot;
        Assert.Contains("component", content);
        Assert.Contains("props", content);
        Assert.Contains("url", content);
    }

    private static InertiaTagHelper CreateTagHelper(Dictionary<string, object?>? page)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());

        if (page != null)
        {
            viewData["Page"] = page;
        }

        var viewContext = new ViewContext(
            actionContext,
            Substitute.For<IView>(),
            viewData,
            Substitute.For<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());

        return new InertiaTagHelper { ViewContext = viewContext };
    }

    private static TagHelperContext CreateContext()
    {
        return new TagHelperContext(
            "inertia",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
    }

    private static TagHelperOutput CreateOutput()
    {
        return new TagHelperOutput(
            "inertia",
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }
}
