using System.Text.Json;
using InertiaCore.Constants;

namespace InertiaCore.Tests.Core.InertiaResponse;

[Trait("Method", "ExecuteAsync")]
public class JsonResponseTests : InertiaResponseTestBase
{
    [Fact]
    public async Task Sets_content_type_to_json()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task Sets_status_code_to_200()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task Sets_x_inertia_response_header()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        Assert.Equal("true", context.Response.Headers[InertiaHeaders.Inertia].ToString());
    }

    [Fact]
    public async Task Page_object_contains_component()
    {
        var response = CreateResponse(component: "Users/Index");
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.Equal("Users/Index", page["component"].GetString());
    }

    [Fact]
    public async Task Page_object_contains_props()
    {
        var props = new Dictionary<string, object?> { ["name"] = "Alice" };
        var response = CreateResponse(props: props);
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        var responseProps = page["props"];
        Assert.Equal("Alice", responseProps.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Page_object_contains_version()
    {
        var response = CreateResponse(version: "abc123");
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.Equal("abc123", page["version"].GetString());
    }

    [Fact]
    public async Task Page_object_contains_url()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext("/users");

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.Equal("/users", page["url"].GetString());
    }

    [Fact]
    public async Task Url_includes_query_string()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext("/search", "?q=test&page=2");

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.Equal("/search?q=test&page=2", page["url"].GetString());
    }

    [Fact]
    public async Task Url_defaults_to_root_when_empty()
    {
        var response = CreateResponse();
        var context = CreateInertiaHttpContext("");

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.Equal("/", page["url"].GetString());
    }

    [Fact]
    public async Task Page_object_keys_are_camel_case()
    {
        var response = CreateResponse(component: "Test");
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.True(page.ContainsKey("component"));
        Assert.True(page.ContainsKey("props"));
        Assert.True(page.ContainsKey("url"));
        Assert.True(page.ContainsKey("version"));
    }

    [Fact]
    public async Task Merges_shared_props_with_page_props()
    {
        var sharedProps = new Dictionary<string, object?> { ["appName"] = "MyApp" };
        var props = new Dictionary<string, object?> { ["user"] = "Alice" };
        var response = CreateResponse(props: props, sharedProps: sharedProps);
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        var responseProps = page["props"];
        Assert.Equal("MyApp", responseProps.GetProperty("appName").GetString());
        Assert.Equal("Alice", responseProps.GetProperty("user").GetString());
    }

    [Fact]
    public async Task Page_props_override_shared_props()
    {
        var sharedProps = new Dictionary<string, object?> { ["key"] = "shared" };
        var props = new Dictionary<string, object?> { ["key"] = "page" };
        var response = CreateResponse(props: props, sharedProps: sharedProps);
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.Equal("page", page["props"].GetProperty("key").GetString());
    }
}
