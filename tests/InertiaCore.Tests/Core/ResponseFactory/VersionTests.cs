namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Method", "Version")]
public class VersionTests : InertiaResponseFactoryTestBase
{
    [Fact]
    public void Returns_null_when_no_version_set()
    {
        var factory = CreateFactory();

        Assert.Null(factory.GetVersion());
    }

    [Fact]
    public void Returns_static_version_from_options()
    {
        var factory = CreateFactory(o => o.Version = "1.0.0");

        Assert.Equal("1.0.0", factory.GetVersion());
    }

    [Fact]
    public void Returns_version_func_from_options()
    {
        var factory = CreateFactory(o => o.VersionFunc = () => "abc123");

        Assert.Equal("abc123", factory.GetVersion());
    }

    [Fact]
    public void VersionFunc_takes_precedence_over_static_version()
    {
        var factory = CreateFactory(o =>
        {
            o.Version = "static";
            o.VersionFunc = () => "dynamic";
        });

        Assert.Equal("dynamic", factory.GetVersion());
    }

    [Fact]
    public void Per_request_version_takes_precedence_over_options()
    {
        var factory = CreateFactory(o => o.VersionFunc = () => "from-options");

        factory.Version("per-request");

        Assert.Equal("per-request", factory.GetVersion());
    }

    [Fact]
    public void Version_included_in_response()
    {
        var factory = CreateFactory(o => o.Version = "v2");

        var response = factory.Render("Home/Index");

        Assert.Equal("v2", response.Version);
    }
}
