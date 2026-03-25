using System.Text.Json;
using InertiaCore.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Core;

/// <summary>
/// Builds the page object and renders as JSON for Inertia requests or as a Razor view for initial page loads.
/// </summary>
public class InertiaResponse : IActionResult, IResult
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    internal string Component { get; }
    internal Dictionary<string, object?> Props { get; }
    internal Dictionary<string, object?> SharedProps { get; }
    internal string RootView { get; }
    internal string? Version { get; }

    private readonly IInertiaFlashService? _flashService;
    private readonly bool _encryptHistory;
    private readonly bool _clearHistory;
    private readonly bool _preserveFragment;
    private readonly Dictionary<string, object?> _viewData = new();

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaResponse"/>.
    /// </summary>
    public InertiaResponse(
        string component,
        Dictionary<string, object?> props,
        Dictionary<string, object?> sharedProps,
        string rootView,
        string? version,
        IInertiaFlashService? flashService = null,
        bool encryptHistory = false,
        bool clearHistory = false,
        bool preserveFragment = false)
    {
        Component = component;
        Props = props;
        SharedProps = sharedProps;
        RootView = rootView;
        Version = version;
        _flashService = flashService;
        _encryptHistory = encryptHistory;
        _clearHistory = clearHistory;
        _preserveFragment = preserveFragment;
    }

    /// <summary>
    /// Adds an additional prop to this response.
    /// </summary>
    public InertiaResponse With(string key, object? value)
    {
        Props[key] = value;
        return this;
    }

    /// <summary>
    /// Adds view data passed to the Razor view on initial page loads.
    /// </summary>
    public InertiaResponse WithViewData(string key, object? value)
    {
        _viewData[key] = value;
        return this;
    }

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context) =>
        await ExecuteAsync(context.HttpContext);

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ConsumeFlashIntoSharedProps();

        var resolver = new PropsResolver(httpContext.RequestServices, httpContext.Request, Component);
        var (resolvedProps, metadata) = await resolver.ResolveAsync(SharedProps, Props);

        var page = BuildPageObject(httpContext, resolvedProps, metadata);

        if (!httpContext.Request.Headers.ContainsKey(InertiaHeaders.Inertia))
        {
            await RenderRazorView(httpContext, page);
            return;
        }

        httpContext.Response.StatusCode = 200;
        httpContext.Response.Headers[InertiaHeaders.Inertia] = "true";
        httpContext.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, page, s_jsonOptions);
    }

    private Dictionary<string, object?> BuildPageObject(
        HttpContext httpContext,
        Dictionary<string, object?> resolvedProps,
        Dictionary<string, object?> metadata)
    {
        var page = new Dictionary<string, object?>
        {
            ["component"] = Component,
            ["props"] = resolvedProps,
            ["url"] = GetUrl(httpContext),
            ["version"] = Version,
        };

        foreach (var (key, value) in metadata)
        {
            page[key] = value;
        }

        if (_encryptHistory)
        {
            page["encryptHistory"] = true;
        }

        if (_clearHistory)
        {
            page["clearHistory"] = true;
        }

        if (_preserveFragment)
        {
            page["preserveFragment"] = true;
        }

        return page;
    }

    private void ConsumeFlashIntoSharedProps()
    {
        var flash = _flashService?.Consume();
        if (flash != null)
        {
            SharedProps["flash"] = flash;
        }
    }

    private static string GetUrl(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var url = $"{request.PathBase}{request.Path}{request.QueryString}";
        return string.IsNullOrEmpty(url) ? "/" : url;
    }

    private async Task RenderRazorView(HttpContext httpContext, Dictionary<string, object?> page)
    {
        var viewEngine = httpContext.RequestServices.GetRequiredService<ICompositeViewEngine>();
        var tempDataFactory = httpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();

        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());
        var viewResult = viewEngine.FindView(actionContext, RootView, isMainPage: true);

        if (!viewResult.Success)
        {
            throw new InvalidOperationException(
                $"Razor view '{RootView}' not found. Searched locations: {string.Join(", ", viewResult.SearchedLocations)}");
        }

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["Page"] = page,
        };

        foreach (var (key, value) in _viewData)
        {
            viewData[key] = value;
        }

        var tempData = tempDataFactory.GetTempData(httpContext);
        await using var writer = new StreamWriter(httpContext.Response.Body, leaveOpen: true);

        var viewContext = new ViewContext(actionContext, viewResult.View, viewData, tempData, writer, new HtmlHelperOptions());
        await viewResult.View.RenderAsync(viewContext);
    }
}
