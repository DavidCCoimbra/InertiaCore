namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Method", "SetRootView")]
public class SetRootViewTests : InertiaResponseFactoryTestBase
{
    [Fact]
    public void Overrides_default_root_view()
    {
        var factory = CreateFactory();

        factory.SetRootView("CustomLayout");
        var response = factory.Render("Home/Index");

        Assert.Equal("CustomLayout", response.RootView);
    }

    [Fact]
    public void Overrides_configured_root_view()
    {
        var factory = CreateFactory(o => o.RootView = "Layout");

        factory.SetRootView("Override");
        var response = factory.Render("Home/Index");

        Assert.Equal("Override", response.RootView);
    }
}
