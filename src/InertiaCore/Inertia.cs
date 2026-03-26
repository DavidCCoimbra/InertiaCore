using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore;

/// <summary>
/// Static convenience helper for Inertia operations. Resolves the scoped factory from the
/// current HttpContext. DI injection of IInertiaResponseFactory is preferred for testability.
/// Requires explicit opt-in via services.AddInertiaStaticHelper().
/// </summary>
public static class Inertia
{
    private static IHttpContextAccessor? s_httpContextAccessor;

    /// <summary>
    /// Initializes the static helper. Called by AddInertiaStaticHelper().
    /// </summary>
    internal static void Initialize(IHttpContextAccessor httpContextAccessor)
    {
        s_httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Renders an Inertia response for the given component.
    /// </summary>
    public static InertiaResponse Render(string component, Dictionary<string, object?>? props = null) =>
        GetFactory().Render(component, props);

    /// <summary>
    /// Renders an Inertia response with anonymous object props.
    /// </summary>
    public static InertiaResponse Render(string component, object props) =>
        GetFactory().Render(component, props);

    /// <summary>
    /// Renders an Inertia response with typed props.
    /// </summary>
    public static InertiaResponse Render<TProps>(string component, TProps props) where TProps : class =>
        GetFactory().Render(component, props);

    /// <summary>
    /// Shares a prop for this request.
    /// </summary>
    public static void Share(string key, object? value) =>
        GetFactory().Share(key, value);

    /// <summary>
    /// Stores flash data for the next response.
    /// </summary>
    public static void Flash(string key, object? value) =>
        GetFactory().Flash(key, value);

    /// <summary>
    /// Returns an external redirect response via 409 + X-Inertia-Location header.
    /// </summary>
    public static IResult Location(string url)
    {
        var context = GetHttpContext();
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.Headers[InertiaHeaders.Location] = url;
        return Results.StatusCode(StatusCodes.Status409Conflict);
    }

    private static IInertiaResponseFactory GetFactory()
    {
        var context = GetHttpContext();
        return context.RequestServices.GetRequiredService<IInertiaResponseFactory>();
    }

    private static HttpContext GetHttpContext()
    {
        if (s_httpContextAccessor?.HttpContext == null)
        {
            throw new InvalidOperationException(
                "Inertia static helper is not initialized. Call services.AddInertiaStaticHelper() " +
                "in your service configuration, or inject IInertiaResponseFactory directly.");
        }

        return s_httpContextAccessor.HttpContext;
    }
}
