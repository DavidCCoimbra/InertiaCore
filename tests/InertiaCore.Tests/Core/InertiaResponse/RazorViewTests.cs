using InertiaCore.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace InertiaCore.Tests.Core.InertiaResponse;

[Trait("Method", "ExecuteAsync")]
public class RazorViewTests : InertiaResponseTestBase
{
    [Fact]
    public async Task Non_inertia_request_renders_razor_view()
    {
        var response = CreateResponse(component: "Home/Index");

        var (context, view) = CreateBrowserHttpContext();

        await response.ExecuteAsync(context);

        await view.Received(1).RenderAsync(Arg.Any<ViewContext>());
    }

    [Fact]
    public async Task Non_inertia_request_still_sets_vary_header()
    {
        var response = CreateResponse();
        var (context, _) = CreateBrowserHttpContext();

        await response.ExecuteAsync(context);

        Assert.Equal(InertiaHeaders.Inertia, context.Response.Headers.Vary.ToString());
    }

    [Fact]
    public async Task Passes_view_data_to_razor_view()
    {
        var response = CreateResponse()
            .WithViewData("title", "My Page")
            .WithViewData("description", "A test page");

        var (context, view) = CreateBrowserHttpContext();

        await response.ExecuteAsync(context);

        await view.Received(1).RenderAsync(Arg.Is<ViewContext>(vc =>
            (string)vc.ViewData["title"]! == "My Page" &&
            (string)vc.ViewData["description"]! == "A test page"));
    }

    [Fact]
    public async Task ExecuteResultAsync_delegates_to_ExecuteAsync()
    {
        var response = CreateResponse(component: "Home/Index");
        var (context, view) = CreateBrowserHttpContext();
        var actionContext = new ActionContext(
            context, new RouteData(), new ActionDescriptor());

        await response.ExecuteResultAsync(actionContext);

        await view.Received(1).RenderAsync(Arg.Any<ViewContext>());
    }

    [Fact]
    public async Task Throws_when_view_not_found()
    {
        var response = CreateResponse(rootView: "NonExistent");
        var (context, _) = CreateBrowserHttpContext(viewFound: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => response.ExecuteAsync(context));

        Assert.Contains("NonExistent", exception.Message);
    }

    private static (DefaultHttpContext Context, IView View) CreateBrowserHttpContext(bool viewFound = true)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var view = Substitute.For<IView>();
        var viewEngine = Substitute.For<ICompositeViewEngine>();

        var viewResult = viewFound
            ? ViewEngineResult.Found("App", view)
            : ViewEngineResult.NotFound("App", new[] { "Views/App.cshtml" });

        viewEngine.FindView(Arg.Any<Microsoft.AspNetCore.Mvc.ActionContext>(), Arg.Any<string>(), Arg.Any<bool>())
            .Returns(viewResult);

        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(Arg.Any<HttpContext>())
            .Returns(Substitute.For<ITempDataDictionary>());

        var services = new ServiceCollection();
        services.AddSingleton(viewEngine);
        services.AddSingleton(tempDataFactory);
        context.RequestServices = services.BuildServiceProvider();

        return (context, view);
    }
}
