using InertiaCore.Configuration;

namespace InertiaCore.Tests.Configuration;

[Trait("Class", "InertiaOptions")]
public class InertiaOptionsTests
{
    [Fact]
    public void Defaults_are_correct()
    {
        var options = new InertiaOptions();

        Assert.Equal("App", options.RootView);
        Assert.Null(options.Version);
        Assert.Null(options.VersionFunc);
        Assert.False(options.EncryptHistory);
        Assert.NotNull(options.Ssr);
    }

    [Fact]
    public void ResolveVersion_returns_null_when_nothing_set()
    {
        var options = new InertiaOptions();

        Assert.Null(options.ResolveVersion());
    }

    [Fact]
    public void ResolveVersion_returns_static_version()
    {
        var options = new InertiaOptions { Version = "1.0.0" };

        Assert.Equal("1.0.0", options.ResolveVersion());
    }

    [Fact]
    public void ResolveVersion_prefers_func_over_static()
    {
        var options = new InertiaOptions
        {
            Version = "static",
            VersionFunc = () => "dynamic",
        };

        Assert.Equal("dynamic", options.ResolveVersion());
    }

    [Fact]
    public void EncryptHistory_can_be_set()
    {
        var options = new InertiaOptions { EncryptHistory = true };

        Assert.True(options.EncryptHistory);
    }

    [Fact]
    public void Ssr_defaults_to_new_instance()
    {
        var options = new InertiaOptions();

        Assert.False(options.Ssr.Enabled);
        Assert.Equal("http://127.0.0.1:13714", options.Ssr.Url);
    }
}
