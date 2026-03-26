namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Method", "Render<TProps>")]
public class RenderGenericTests : InertiaResponseFactoryTestBase
{
    public record DashboardProps(string Title, int Count);

    public class UserProps
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [Fact]
    public void Renders_with_record_props()
    {
        var factory = CreateFactory();
        var props = new DashboardProps("Dashboard", 42);

        var response = factory.Render("Dashboard/Index", props);

        Assert.Equal("Dashboard/Index", response.Component);
        Assert.Equal("Dashboard", response.Props["Title"]);
        Assert.Equal(42, response.Props["Count"]);
    }

    [Fact]
    public void Renders_with_class_props()
    {
        var factory = CreateFactory();
        var props = new UserProps { Name = "Alice", Age = 30 };

        var response = factory.Render("Users/Show", props);

        Assert.Equal("Alice", response.Props["Name"]);
        Assert.Equal(30, response.Props["Age"]);
    }

    [Fact]
    public void Backward_compatible_with_dictionary()
    {
        var factory = CreateFactory();
        var dict = new Dictionary<string, object?> { ["key"] = "value" };

        var response = factory.Render("Test", dict);

        Assert.Equal("value", response.Props["key"]);
    }

    [Fact]
    public void Backward_compatible_with_anonymous_object()
    {
        var factory = CreateFactory();

        var response = factory.Render("Test", new { Name = "Bob", Age = 25 });

        Assert.Equal("Bob", response.Props["Name"]);
        Assert.Equal(25, response.Props["Age"]);
    }
}
