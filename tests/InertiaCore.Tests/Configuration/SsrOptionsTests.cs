using InertiaCore.Configuration;

namespace InertiaCore.Tests.Configuration;

[Trait("Class", "SsrOptions")]
public class SsrOptionsTests
{
    [Fact]
    public void Defaults_are_correct()
    {
        var options = new SsrOptions();

        Assert.False(options.Enabled);
        Assert.Equal("http://127.0.0.1:13714", options.Url);
    }

    [Fact]
    public void Properties_can_be_set()
    {
        var options = new SsrOptions
        {
            Enabled = true,
            Url = "http://localhost:9000",
        };

        Assert.True(options.Enabled);
        Assert.Equal("http://localhost:9000", options.Url);
    }
}
