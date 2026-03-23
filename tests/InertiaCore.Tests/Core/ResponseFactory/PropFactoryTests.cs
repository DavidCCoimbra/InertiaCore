using InertiaCore.Props;

namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Class", "InertiaResponseFactory")]
public class PropFactoryTests : InertiaResponseFactoryTestBase
{
    // -- Always --

    [Fact]
    public void Always_with_value_returns_AlwaysProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always("hello");

        Assert.IsType<AlwaysProp>(prop);
    }

    [Fact]
    public void Always_with_callback_returns_AlwaysProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always(() => "computed");

        Assert.IsType<AlwaysProp>(prop);
    }

    [Fact]
    public void Always_with_async_callback_returns_AlwaysProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always(() => Task.FromResult<object?>("async"));

        Assert.IsType<AlwaysProp>(prop);
    }

    // -- Optional --

    [Fact]
    public void Optional_returns_OptionalProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Optional(() => "lazy");

        Assert.IsType<OptionalProp>(prop);
    }

    [Fact]
    public void Optional_async_returns_OptionalProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Optional(() => Task.FromResult<object?>("async"));

        Assert.IsType<OptionalProp>(prop);
    }

    // -- Defer --

    [Fact]
    public void Defer_returns_DeferProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => "heavy");

        Assert.IsType<DeferProp>(prop);
        Assert.Equal("default", prop.Defer.Group());
    }

    [Fact]
    public void Defer_with_group_sets_group()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => "heavy", group: "charts");

        Assert.Equal("charts", prop.Defer.Group());
    }

    [Fact]
    public void Defer_async_returns_DeferProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => Task.FromResult<object?>("async"));

        Assert.IsType<DeferProp>(prop);
    }

    // -- Merge --

    [Fact]
    public void Merge_with_value_returns_MergeProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge(new[] { 1, 2, 3 });

        Assert.IsType<MergeProp>(prop);
        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Merge_with_callback_returns_MergeProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge(() => new[] { 1, 2, 3 });

        Assert.IsType<MergeProp>(prop);
    }

    [Fact]
    public void Merge_async_returns_MergeProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge(() => Task.FromResult<object?>(new[] { 1, 2, 3 }));

        Assert.IsType<MergeProp>(prop);
    }

    // -- Once --

    [Fact]
    public void Once_returns_OnceProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Once(() => "permissions");

        Assert.IsType<OnceProp>(prop);
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void Once_async_returns_OnceProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Once(() => Task.FromResult<object?>("async"));

        Assert.IsType<OnceProp>(prop);
    }

    // -- ShareOnce --

    [Fact]
    public void ShareOnce_adds_OnceProp_to_shared()
    {
        var factory = CreateFactory();

        factory.ShareOnce("permissions", () => (object?)"admin");

        var shared = factory.GetShared("permissions");
        Assert.IsType<OnceProp>(shared);
    }

    [Fact]
    public void ShareOnce_async_adds_OnceProp_to_shared()
    {
        var factory = CreateFactory();

        factory.ShareOnce("permissions", () => Task.FromResult<object?>("admin"));

        var shared = factory.GetShared("permissions");
        Assert.IsType<OnceProp>(shared);
    }

    // -- Usable in Render --

    [Fact]
    public void Factory_methods_usable_in_render_props()
    {
        var factory = CreateFactory();

        var response = factory.Render("Home/Index", new Dictionary<string, object?>
        {
            ["flash"] = InertiaCore.Core.InertiaResponseFactory.Always("success"),
            ["stats"] = InertiaCore.Core.InertiaResponseFactory.Defer(() => "heavy", "analytics"),
            ["items"] = InertiaCore.Core.InertiaResponseFactory.Merge(new[] { 1, 2 }),
            ["lazy"] = InertiaCore.Core.InertiaResponseFactory.Optional(() => "optional"),
            ["perms"] = InertiaCore.Core.InertiaResponseFactory.Once(() => "admin"),
        });

        Assert.IsType<AlwaysProp>(response.Props["flash"]);
        Assert.IsType<DeferProp>(response.Props["stats"]);
        Assert.IsType<MergeProp>(response.Props["items"]);
        Assert.IsType<OptionalProp>(response.Props["lazy"]);
        Assert.IsType<OnceProp>(response.Props["perms"]);
    }
}
