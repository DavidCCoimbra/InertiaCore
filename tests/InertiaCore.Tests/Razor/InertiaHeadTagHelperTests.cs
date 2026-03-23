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

[Trait("Class", "InertiaHeadTagHelper")]
public class InertiaHeadTagHelperTests
{
    [Fact]
    public void Removes_tag_name()
    {
        var tagHelper = CreateTagHelper(headContent: null);
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        Assert.Null(output.TagName);
    }

    [Fact]
    public void Renders_head_content_when_present()
    {
        var tagHelper = CreateTagHelper(headContent: "<title>My Page</title><meta name=\"description\" content=\"test\">");
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        var content = output.Content.GetContent();
        Assert.Contains("<title>My Page</title>", content);
        Assert.Contains("<meta name=\"description\"", content);
    }

    [Fact]
    public void Renders_nothing_when_head_content_missing()
    {
        var tagHelper = CreateTagHelper(headContent: null);
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        Assert.False(output.IsContentModified);
    }

    [Fact]
    public void Renders_nothing_when_head_content_is_wrong_type()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["InertiaHead"] = 42,
        };
        var viewContext = new ViewContext(
            actionContext,
            Substitute.For<IView>(),
            viewData,
            Substitute.For<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());

        var tagHelper = new InertiaHeadTagHelper { ViewContext = viewContext };
        var output = CreateOutput();

        tagHelper.Process(CreateContext(), output);

        Assert.False(output.IsContentModified);
    }

    private static InertiaHeadTagHelper CreateTagHelper(string? headContent)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());

        if (headContent != null)
        {
            viewData["InertiaHead"] = headContent;
        }

        var viewContext = new ViewContext(
            actionContext,
            Substitute.For<IView>(),
            viewData,
            Substitute.For<ITempDataDictionary>(),
            TextWriter.Null,
            new HtmlHelperOptions());

        return new InertiaHeadTagHelper { ViewContext = viewContext };
    }

    private static TagHelperContext CreateContext()
    {
        return new TagHelperContext(
            "inertia-head",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
    }

    private static TagHelperOutput CreateOutput()
    {
        return new TagHelperOutput(
            "inertia-head",
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }
}
