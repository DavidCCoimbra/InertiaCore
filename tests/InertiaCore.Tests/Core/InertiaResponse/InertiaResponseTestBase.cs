using System.Text.Json;
using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Tests.Core.InertiaResponse;

[Trait("Class", "InertiaResponse")]
public abstract class InertiaResponseTestBase
{
    protected static InertiaCore.Core.InertiaResponse CreateResponse(
        string component = "Test/Component",
        Dictionary<string, object?>? props = null,
        Dictionary<string, object?>? sharedProps = null,
        string rootView = "App",
        string? version = null,
        IInertiaFlashService? flashService = null)
    {
        var context = new InertiaResponseContext(
            RootView: rootView,
            Version: version,
            FlashService: flashService);

        return new InertiaCore.Core.InertiaResponse(
            component: component,
            props: props ?? new Dictionary<string, object?>(),
            sharedProps: sharedProps ?? new Dictionary<string, object?>(),
            context: context);
    }

    protected static DefaultHttpContext CreateInertiaHttpContext(string path = "/", string? queryString = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";
        context.Request.Path = path;
        if (queryString != null)
        {
            context.Request.QueryString = new QueryString(queryString);
        }

        context.Response.Body = new MemoryStream();
        return context;
    }

    protected static async Task<Dictionary<string, JsonElement>> ReadJsonResponse(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(context.Response.Body)
            ?? throw new InvalidOperationException("Failed to deserialize JSON response");
    }
}
