using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Ssr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InertiaCore.Core;

/// <summary>
/// Builds the page object and renders as JSON for Inertia requests or as a Razor view for initial page loads.
/// </summary>
public class InertiaResponse : IActionResult, IResult
{
    private static readonly JsonSerializerOptions s_jsonOptions = InertiaJsonOptions.CamelCase;

    internal string Component { get; }
    internal Dictionary<string, object?> Props { get; }
    internal Dictionary<string, object?> SharedProps { get; }
    internal string RootView => _context.RootView;
    internal string? Version => _context.Version;

    private readonly InertiaResponseContext _context;
    private readonly Dictionary<string, object?> _viewData = new();

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaResponse"/>.
    /// </summary>
    public InertiaResponse(
        string component,
        Dictionary<string, object?> props,
        Dictionary<string, object?> sharedProps,
        InertiaResponseContext context)
    {
        Component = component;
        Props = props;
        SharedProps = sharedProps;
        _context = context;
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
        var (resolvedProps, metadata) = await resolver.ResolveAsync(SharedProps, Props).ConfigureAwait(false);

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
            ["version"] = _context.Version,
        };

        foreach (var (key, value) in metadata)
        {
            page[key] = value;
        }

        if (_context.EncryptHistory)
        {
            page["encryptHistory"] = true;
        }

        if (_context.ClearHistory)
        {
            page["clearHistory"] = true;
        }

        if (_context.PreserveFragment)
        {
            page["preserveFragment"] = true;
        }

        return page;
    }

    private void ConsumeFlashIntoSharedProps()
    {
        var flash = _context.FlashService?.Consume();
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
        var viewResult = viewEngine.FindView(actionContext, _context.RootView, isMainPage: true);

        if (!viewResult.Success)
        {
            throw new InvalidOperationException(
                $"Razor view '{_context.RootView}' not found. Searched locations: {string.Join(", ", viewResult.SearchedLocations)}");
        }

        var ssrResponse = await TrySsrRenderAsync(httpContext, page);

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            ["Page"] = page,
        };

        if (ssrResponse != null)
        {
            viewData["InertiaHead"] = ssrResponse.Head;
            viewData["InertiaBody"] = ssrResponse.Body;
        }

        foreach (var (key, value) in _viewData)
        {
            viewData[key] = value;
        }

        var tempData = tempDataFactory.GetTempData(httpContext);
        await using var writer = new StreamWriter(httpContext.Response.Body, leaveOpen: true);

        var viewContext = new ViewContext(actionContext, viewResult.View, viewData, tempData, writer, new HtmlHelperOptions());
        await viewResult.View.RenderAsync(viewContext);
    }

    private async Task<SsrResponse?> TrySsrRenderAsync(
        HttpContext httpContext,
        Dictionary<string, object?> page)
    {
        if (_context.SsrGateway == null)
        {
            return null;
        }

        if (IsPathExcludedFromSsr(httpContext))
        {
            return null;
        }

        try
        {
            return await _context.SsrGateway.RenderAsync(page, httpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _context.Logger?.LogWarning(ex, "SSR rendering failed unexpectedly, falling back to CSR");
            return null;
        }
    }

    private bool IsPathExcludedFromSsr(HttpContext httpContext)
    {
        if (_context.SsrExcludedPaths is not { Length: > 0 })
        {
            return false;
        }

        var path = httpContext.Request.Path.Value ?? "/";
        return _context.SsrExcludedPaths!.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }
}
