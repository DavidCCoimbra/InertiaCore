using InertiaCore.Testing;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Integration;

[Trait("Category", "Integration")]
public class EndpointFilterTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public EndpointFilterTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Minimal_api_returns_inertia_response()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        inertia
            .IsInertiaResponse()
            .IsSuccessful()
            .HasComponent("Home/Index");
    }

    [Fact]
    public async Task MapInertia_shortcut_works_as_minimal_api()
    {
        var inertia = await _client.GetInertiaAssertAsync("/about", "1.0.0");

        inertia
            .IsInertiaResponse()
            .HasComponent("About/Index")
            .HasProp("title", "About Us");
    }

    [Fact]
    public async Task Minimal_api_shared_props_from_middleware()
    {
        var inertia = await _client.GetInertiaAssertAsync("/", "1.0.0");

        // errors shared by InertiaErrorService via middleware
        inertia.HasSharedProp("errors");
    }
}
