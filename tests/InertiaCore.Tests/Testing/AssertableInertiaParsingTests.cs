using System.Net;
using System.Net.Http.Headers;
using InertiaCore.Testing;

namespace InertiaCore.Tests.Testing;

[Trait("Class", "AssertableInertia")]
public class AssertableInertiaParsingTests
{
    [Fact]
    public async Task FromResponse_throws_on_html_without_data_page()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html><body>No inertia here</body></html>"),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => AssertableInertia.FromResponseAsync(response));
    }

    [Fact]
    public async Task FromResponse_parses_json_content_type()
    {
        var json = """{"component":"Test","url":"/","version":"1","props":{}}""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.HasComponent("Test").HasUrl("/");
    }

    [Fact]
    public async Task FromResponse_parses_data_page_from_html()
    {
        var html = """<html><body><div id="app" data-page="{&quot;component&quot;:&quot;Home&quot;,&quot;url&quot;:&quot;/&quot;,&quot;version&quot;:&quot;1&quot;,&quot;props&quot;:{}}"></div></body></html>""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");

        var inertia = await AssertableInertia.FromResponseAsync(response);

        inertia.HasComponent("Home").HasUrl("/");
    }
}
