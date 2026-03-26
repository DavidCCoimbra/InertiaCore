using InertiaCore.Configuration;

namespace InertiaCore.Tests.Configuration;

[Trait("Class", "InertiaOptionsValidator")]
public class InertiaOptionsValidatorTests
{
    private readonly InertiaOptionsValidator _validator = new();

    [Fact]
    public void Valid_defaults_pass()
    {
        var result = _validator.Validate(null, new InertiaOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Empty_root_view_fails()
    {
        var options = new InertiaOptions { RootView = "" };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("RootView", result.FailureMessage);
    }

    [Fact]
    public void Empty_ssr_url_when_enabled_fails()
    {
        var options = new InertiaOptions { Ssr = { Enabled = true, Url = "" } };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Url", result.FailureMessage);
    }

    [Fact]
    public void Invalid_ssr_url_when_enabled_fails()
    {
        var options = new InertiaOptions { Ssr = { Enabled = true, Url = "not-a-url" } };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("valid URI", result.FailureMessage);
    }

    [Fact]
    public void Zero_timeout_fails()
    {
        var options = new InertiaOptions { Ssr = { TimeoutSeconds = 0 } };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("TimeoutSeconds", result.FailureMessage);
    }

    [Fact]
    public void Negative_timeout_fails()
    {
        var options = new InertiaOptions { Ssr = { TimeoutSeconds = -1 } };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Disabled_ssr_skips_url_validation()
    {
        var options = new InertiaOptions { Ssr = { Enabled = false, Url = "invalid" } };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Valid_ssr_config_passes()
    {
        var options = new InertiaOptions
        {
            Ssr = { Enabled = true, Url = "http://localhost:13714", TimeoutSeconds = 10 },
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
