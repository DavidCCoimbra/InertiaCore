using InertiaCore.Core;

namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Method", "Render")]
public class RenderTests : InertiaResponseFactoryTestBase
{
    [Fact]
    public void Returns_InertiaResponse_with_correct_component()
    {
        var factory = CreateFactory();

        var response = factory.Render("Users/Index");

        Assert.Equal("Users/Index", response.Component);
    }

    [Fact]
    public void Passes_props_dictionary_to_response()
    {
        var factory = CreateFactory();
        var props = new Dictionary<string, object?> { ["name"] = "Alice" };

        var response = factory.Render("Users/Show", props);

        Assert.Equal("Alice", response.Props["name"]);
    }

    [Fact]
    public void Uses_empty_props_when_null()
    {
        var factory = CreateFactory();

        var response = factory.Render("Home/Index");

        Assert.Empty(response.Props);
    }

    [Fact]
    public void Converts_anonymous_object_to_props_dictionary()
    {
        var factory = CreateFactory();

        var response = factory.Render("Users/Show", new { Name = "Alice", Age = 30 });

        Assert.Equal("Alice", response.Props["Name"]);
        Assert.Equal(30, response.Props["Age"]);
    }

    [Fact]
    public void Includes_shared_props_in_response()
    {
        var factory = CreateFactory();
        factory.Share("appName", "MyApp");

        var response = factory.Render("Home/Index");

        Assert.Equal("MyApp", response.SharedProps["appName"]);
    }

    [Fact]
    public void Shared_props_are_copied_not_referenced()
    {
        var factory = CreateFactory();
        factory.Share("key", "value1");

        var response1 = factory.Render("Page1");
        factory.Share("key", "value2");
        var response2 = factory.Render("Page2");

        Assert.Equal("value1", response1.SharedProps["key"]);
        Assert.Equal("value2", response2.SharedProps["key"]);
    }

    [Fact]
    public void Uses_configured_root_view()
    {
        var factory = CreateFactory(o => o.RootView = "Layout");

        var response = factory.Render("Home/Index");

        Assert.Equal("Layout", response.RootView);
    }

    [Fact]
    public void Uses_default_root_view()
    {
        var factory = CreateFactory();

        var response = factory.Render("Home/Index");

        Assert.Equal("App", response.RootView);
    }

    // -- Render<TProps> --

    [Fact]
    public void Render_generic_converts_typed_props_to_dictionary()
    {
        var factory = CreateFactory();

        var response = factory.Render("Users/Show", new UserProps("Alice", 30));

        Assert.Equal("Alice", response.Props["Name"]);
        Assert.Equal(30, response.Props["Age"]);
    }

    [Fact]
    public void Render_generic_available_via_interface()
    {
        IInertiaResponseFactory factory = CreateFactory();

        var response = factory.Render("Users/Show", new UserProps("Bob", 25));

        Assert.Equal("Users/Show", response.Component);
        Assert.Equal("Bob", response.Props["Name"]);
    }

    [Fact]
    public void Render_generic_with_record_preserves_all_properties()
    {
        var factory = CreateFactory();
        var props = new DashboardProps(
            Title: "Dashboard",
            Items: [1, 2, 3],
            IsAdmin: true);

        var response = factory.Render("Dashboard/Index", props);

        Assert.Equal("Dashboard", response.Props["Title"]);
        Assert.Equal(new[] { 1, 2, 3 }, response.Props["Items"]);
        Assert.Equal(true, response.Props["IsAdmin"]);
    }

    private record UserProps(string Name, int Age);
    private record DashboardProps(string Title, int[] Items, bool IsAdmin);
}
