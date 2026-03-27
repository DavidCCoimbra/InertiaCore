using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Tests.Core;

[Trait("Class", "Inertia")]
public class InertiaStaticHelperTests
{
    [Fact]
    public void Render_delegates_to_factory()
    {
        var (_, httpContextAccessor) = SetupStaticHelper();

        var response = Inertia.Render("Test/Component", new { Name = "Alice" });

        Assert.Equal("Test/Component", response.Component);
    }

    [Fact]
    public void Render_with_dictionary_works()
    {
        SetupStaticHelper();

        var response = Inertia.Render("Test", new Dictionary<string, object?> { ["key"] = "value" });

        Assert.Equal("value", response.Props["key"]);
    }

    [Fact]
    public void Render_generic_works()
    {
        SetupStaticHelper();

        var response = Inertia.Render("Test", new TestProps("Alice", 30));

        Assert.Equal("Alice", response.Props["name"]);
    }

    [Fact]
    public void Share_delegates_to_factory()
    {
        var (factory, _) = SetupStaticHelper();

        Inertia.Share("appName", "TestApp");

        Assert.Equal("TestApp", factory.GetShared("appName"));
    }

    [Fact]
    public void Flash_delegates_to_factory()
    {
        var (factory, _) = SetupStaticHelper();

        Inertia.Flash("success", "Done!");

        // Flash is delegated to flash service which is mocked — just verify no exception
    }

    [Fact]
    public void Throws_when_not_initialized()
    {
        Inertia.Initialize(Substitute.For<IHttpContextAccessor>());

        Assert.Throws<InvalidOperationException>(() =>
            Inertia.Render("Test"));
    }

    private static (IInertiaResponseFactory Factory, IHttpContextAccessor Accessor) SetupStaticHelper()
    {
        var flashService = Substitute.For<IInertiaFlashService>();
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var factory = new InertiaCore.Core.InertiaResponseFactory(
            Options.Create(new InertiaCore.Configuration.InertiaOptions()),
            flashService,
            accessor);
        var services = new ServiceCollection();
        services.AddSingleton<IInertiaResponseFactory>(factory);
        httpContext.RequestServices = services.BuildServiceProvider();

        Inertia.Initialize(accessor);

        return (factory, accessor);
    }

    // -- Prop factory methods (no initialization required) --

    [Fact]
    public void Always_works_without_initialization()
    {
        var prop = Inertia.Always("hello");

        Assert.IsType<InertiaCore.Props.AlwaysProp<string>>(prop);
    }

    [Fact]
    public void Defer_works_without_initialization()
    {
        var prop = Inertia.Defer(() => "heavy", group: "analytics");

        Assert.IsType<InertiaCore.Props.DeferProp<string>>(prop);
        Assert.Equal("analytics", prop.Defer.Group());
    }

    [Fact]
    public void Merge_works_without_initialization()
    {
        var prop = Inertia.Merge(new[] { 1, 2, 3 });

        Assert.IsType<InertiaCore.Props.MergeProp<int[]>>(prop);
        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Once_works_without_initialization()
    {
        var prop = Inertia.Once(() => "permissions");

        Assert.IsType<InertiaCore.Props.OnceProp<string>>(prop);
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void Optional_works_without_initialization()
    {
        var prop = Inertia.Optional(() => "lazy");

        Assert.IsType<InertiaCore.Props.OptionalProp<string>>(prop);
    }

    [Fact]
    public void Scroll_works_without_initialization()
    {
        var prop = Inertia.Scroll(new[] { "a", "b" });

        Assert.IsType<InertiaCore.Props.ScrollProp<string[]>>(prop);
    }

    [Fact]
    public void Non_generic_factory_methods_work_without_initialization()
    {
        var always = Inertia.Always((object?)"hello");
        var defer = Inertia.Defer(() => (object?)"heavy");
        var merge = Inertia.Merge((object?)new[] { 1 });
        var once = Inertia.Once(() => (object?)"perms");
        var optional = Inertia.Optional(() => (object?)"lazy");

        Assert.IsType<InertiaCore.Props.AlwaysProp>(always);
        Assert.IsType<InertiaCore.Props.DeferProp>(defer);
        Assert.IsType<InertiaCore.Props.MergeProp>(merge);
        Assert.IsType<InertiaCore.Props.OnceProp>(once);
        Assert.IsType<InertiaCore.Props.OptionalProp>(optional);
    }

    private record TestProps(string Name, int Age);
}
