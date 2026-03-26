using InertiaCore.Contracts;
using InertiaCore.Props;
using InertiaCore.Props.Behaviors;
using NSubstitute;

namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Class", "InertiaResponseFactory")]
public class PropFactoryTests : InertiaResponseFactoryTestBase
{
    // -- Always (non-generic) --

    [Fact]
    public void Always_with_value_returns_AlwaysProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always((object?)"hello");

        Assert.IsType<AlwaysProp>(prop);
    }

    [Fact]
    public void Always_with_callback_returns_AlwaysProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always(() => (object?)"computed");

        Assert.IsType<AlwaysProp>(prop);
    }

    [Fact]
    public void Always_with_async_callback_returns_AlwaysProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always(() => Task.FromResult<object?>("async"));

        Assert.IsType<AlwaysProp>(prop);
    }

    [Fact]
    public void Always_with_service_provider_callback_returns_AlwaysProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always((IServiceProvider _) => (object?)"sp");

        Assert.IsType<AlwaysProp>(prop);
    }

    [Fact]
    public void Always_with_async_service_provider_callback_returns_AlwaysProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always((IServiceProvider _) => Task.FromResult<object?>("sp"));

        Assert.IsType<AlwaysProp>(prop);
    }

    // -- Always<T> (generic) --

    [Fact]
    public void Always_generic_with_value_returns_AlwaysPropT()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always("hello");

        Assert.IsType<AlwaysProp<string>>(prop);
    }

    [Fact]
    public void Always_generic_with_callback_returns_AlwaysPropT()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always(() => "computed");

        Assert.IsType<AlwaysProp<string>>(prop);
    }

    [Fact]
    public async Task Always_generic_resolves_typed_value()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always(() => 42);
        var services = Substitute.For<IServiceProvider>();

        var result = await prop.ResolveAsync(services);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Always_generic_with_service_provider_returns_AlwaysPropT()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always((IServiceProvider _) => "sp-value");

        Assert.IsType<AlwaysProp<string>>(prop);
    }

    // -- Optional (non-generic) --

    [Fact]
    public void Optional_returns_OptionalProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Optional(() => (object?)"lazy");

        Assert.IsType<OptionalProp>(prop);
    }

    [Fact]
    public void Optional_async_returns_OptionalProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Optional(() => Task.FromResult<object?>("async"));

        Assert.IsType<OptionalProp>(prop);
    }

    [Fact]
    public void Optional_with_service_provider_returns_OptionalProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Optional((IServiceProvider _) => (object?)"sp");

        Assert.IsType<OptionalProp>(prop);
    }

    // -- Optional<T> (generic) --

    [Fact]
    public void Optional_generic_returns_OptionalPropT()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Optional(() => "lazy");

        Assert.IsType<OptionalProp<string>>(prop);
    }

    [Fact]
    public async Task Optional_generic_resolves_typed_value()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Optional(() => new[] { "a", "b" });
        var services = Substitute.For<IServiceProvider>();

        var result = await prop.ResolveAsync(services);

        Assert.Equal(new[] { "a", "b" }, result);
    }

    // -- Defer (non-generic) --

    [Fact]
    public void Defer_returns_DeferProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => (object?)"heavy");

        Assert.IsType<DeferProp>(prop);
        Assert.Equal("default", prop.Defer.Group());
    }

    [Fact]
    public void Defer_with_group_sets_group()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => (object?)"heavy", group: "charts");

        Assert.Equal("charts", prop.Defer.Group());
    }

    [Fact]
    public void Defer_async_returns_DeferProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => Task.FromResult<object?>("async"));

        Assert.IsType<DeferProp>(prop);
    }

    [Fact]
    public void Defer_with_service_provider_returns_DeferProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer((IServiceProvider _) => (object?)"sp");

        Assert.IsType<DeferProp>(prop);
    }

    // -- Defer<T> (generic) --

    [Fact]
    public void Defer_generic_returns_DeferPropT()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => "heavy");

        Assert.IsType<DeferProp<string>>(prop);
        Assert.Equal("default", prop.Defer.Group());
    }

    [Fact]
    public void Defer_generic_with_group_sets_group()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => "heavy", group: "analytics");

        Assert.Equal("analytics", prop.Defer.Group());
    }

    [Fact]
    public async Task Defer_generic_resolves_typed_value()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => 99);
        var services = Substitute.For<IServiceProvider>();

        var result = await prop.ResolveAsync(services);

        Assert.Equal(99, result);
    }

    // -- Merge (non-generic) --

    [Fact]
    public void Merge_with_value_returns_MergeProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge((object?)new[] { 1, 2, 3 });

        Assert.IsType<MergeProp>(prop);
        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Merge_with_callback_returns_MergeProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge(() => (object?)new[] { 1, 2, 3 });

        Assert.IsType<MergeProp>(prop);
    }

    [Fact]
    public void Merge_async_returns_MergeProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge(() => Task.FromResult<object?>(new[] { 1, 2, 3 }));

        Assert.IsType<MergeProp>(prop);
    }

    [Fact]
    public void Merge_with_service_provider_returns_MergeProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge((IServiceProvider _) => (object?)new[] { 1 });

        Assert.IsType<MergeProp>(prop);
    }

    // -- Merge<T> (generic) --

    [Fact]
    public void Merge_generic_with_value_returns_MergePropT()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge(new[] { 1, 2, 3 });

        Assert.IsType<MergeProp<int[]>>(prop);
        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Merge_generic_with_callback_returns_MergePropT()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge(() => new[] { 1, 2, 3 });

        Assert.IsType<MergeProp<int[]>>(prop);
    }

    [Fact]
    public async Task Merge_generic_resolves_typed_value()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Merge(new[] { "a", "b" });
        var services = Substitute.For<IServiceProvider>();

        var result = await prop.ResolveAsync(services);

        Assert.Equal(new[] { "a", "b" }, result);
    }

    // -- Once (non-generic) --

    [Fact]
    public void Once_returns_OnceProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Once(() => (object?)"permissions");

        Assert.IsType<OnceProp>(prop);
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void Once_async_returns_OnceProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Once(() => Task.FromResult<object?>("async"));

        Assert.IsType<OnceProp>(prop);
    }

    [Fact]
    public void Once_with_service_provider_returns_OnceProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Once((IServiceProvider _) => (object?)"sp");

        Assert.IsType<OnceProp>(prop);
    }

    // -- Once<T> (generic) --

    [Fact]
    public void Once_generic_returns_OncePropT()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Once(() => "permissions");

        Assert.IsType<OnceProp<string>>(prop);
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public async Task Once_generic_resolves_typed_value()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Once(() => new[] { "read", "write" });
        var services = Substitute.For<IServiceProvider>();

        var result = await prop.ResolveAsync(services);

        Assert.Equal(new[] { "read", "write" }, result);
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

    // -- Scroll --

    [Fact]
    public void Scroll_with_value_returns_ScrollProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Scroll(new[] { 1, 2, 3 });

        Assert.IsType<ScrollProp<int[]>>(prop);
        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Scroll_with_callback_returns_ScrollProp()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Scroll(() => new[] { 1, 2, 3 });

        Assert.IsType<ScrollProp<int[]>>(prop);
    }

    // -- Generic factory methods usable in Render props dictionary --

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

        // Generic overloads win — verify they're IInertiaProp
        Assert.IsAssignableFrom<IInertiaProp>(response.Props["flash"]);
        Assert.IsAssignableFrom<IInertiaProp>(response.Props["stats"]);
        Assert.IsAssignableFrom<IInertiaProp>(response.Props["items"]);
        Assert.IsAssignableFrom<IInertiaProp>(response.Props["lazy"]);
        Assert.IsAssignableFrom<IInertiaProp>(response.Props["perms"]);

        // Verify correct generic types
        Assert.IsType<AlwaysProp<string>>(response.Props["flash"]);
        Assert.IsType<DeferProp<string>>(response.Props["stats"]);
        Assert.IsType<MergeProp<int[]>>(response.Props["items"]);
        Assert.IsType<OptionalProp<string>>(response.Props["lazy"]);
        Assert.IsType<OnceProp<string>>(response.Props["perms"]);
    }

    // -- Type inference verification --

    [Fact]
    public void Generic_overload_wins_when_type_is_specific()
    {
        // Without explicit (object?) cast, generic overload is selected
        var always = InertiaCore.Core.InertiaResponseFactory.Always("hello");
        var defer = InertiaCore.Core.InertiaResponseFactory.Defer(() => 42);
        var merge = InertiaCore.Core.InertiaResponseFactory.Merge(new[] { "a" });
        var once = InertiaCore.Core.InertiaResponseFactory.Once(() => true);
        var optional = InertiaCore.Core.InertiaResponseFactory.Optional(() => 3.14);

        Assert.IsType<AlwaysProp<string>>(always);
        Assert.IsType<DeferProp<int>>(defer);
        Assert.IsType<MergeProp<string[]>>(merge);
        Assert.IsType<OnceProp<bool>>(once);
        Assert.IsType<OptionalProp<double>>(optional);
    }

    [Fact]
    public void Non_generic_overload_wins_with_explicit_object_cast()
    {
        var always = InertiaCore.Core.InertiaResponseFactory.Always((object?)"hello");
        var defer = InertiaCore.Core.InertiaResponseFactory.Defer(() => (object?)42);
        var merge = InertiaCore.Core.InertiaResponseFactory.Merge((object?)new[] { "a" });
        var once = InertiaCore.Core.InertiaResponseFactory.Once(() => (object?)true);
        var optional = InertiaCore.Core.InertiaResponseFactory.Optional(() => (object?)3.14);

        Assert.IsType<AlwaysProp>(always);
        Assert.IsType<DeferProp>(defer);
        Assert.IsType<MergeProp>(merge);
        Assert.IsType<OnceProp>(once);
        Assert.IsType<OptionalProp>(optional);
    }
}
