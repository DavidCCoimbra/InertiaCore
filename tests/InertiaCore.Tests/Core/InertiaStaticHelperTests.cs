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

        Assert.Equal("Alice", response.Props["Name"]);
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
        var factory = new InertiaCore.Core.InertiaResponseFactory(
            Options.Create(new InertiaCore.Configuration.InertiaOptions()),
            flashService);

        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();
        services.AddSingleton<IInertiaResponseFactory>(factory);
        httpContext.RequestServices = services.BuildServiceProvider();

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        Inertia.Initialize(accessor);

        return (factory, accessor);
    }

    private record TestProps(string Name, int Age);
}
