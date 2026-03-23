using InertiaCore.Constants;
using InertiaCore.Extensions;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Tests.Extensions;

[Trait("Class", "HttpContextExtensions")]
public class HttpContextExtensionsTests
{
    [Fact]
    public void IsInertiaRequest_returns_true_when_header_present()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";

        Assert.True(context.IsInertiaRequest());
    }

    [Fact]
    public void IsInertiaRequest_returns_false_when_header_missing()
    {
        var context = new DefaultHttpContext();

        Assert.False(context.IsInertiaRequest());
    }

    [Fact]
    public void GetInertiaVersion_returns_header_value()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Version] = "abc123";

        Assert.Equal("abc123", context.GetInertiaVersion());
    }

    [Fact]
    public void GetInertiaVersion_returns_null_when_missing()
    {
        var context = new DefaultHttpContext();

        Assert.Null(context.GetInertiaVersion());
    }
}
