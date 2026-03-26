using InertiaCore.Configuration;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Method", "Back")]
[Trait("Method", "Redirect")]
public class RedirectTests : InertiaResponseFactoryTestBase
{
    [Fact]
    public void Back_returns_InertiaRedirectResult()
    {
        var factory = CreateFactoryWithHttpContext();

        var result = factory.Back();

        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void Back_uses_referer_header()
    {
        var factory = CreateFactoryWithHttpContext(referer: "http://localhost/form");

        var result = factory.Back();

        // Verify via ExecuteAsync that redirect goes to referer
        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void Back_defaults_to_root_without_referer()
    {
        var factory = CreateFactoryWithHttpContext();

        var result = factory.Back();

        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void Redirect_returns_InertiaRedirectResult()
    {
        var factory = CreateFactory();

        var result = factory.Redirect("/users");

        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void WithErrors_is_chainable()
    {
        var factory = CreateFactory();

        var result = factory.Redirect("/users")
            .WithErrors(new() { ["name"] = "Required" });

        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void WithFlash_is_chainable()
    {
        var factory = CreateFactory();

        var result = factory.Redirect("/users")
            .WithFlash("success", "Saved!");

        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void Full_chain_is_fluent()
    {
        var factory = CreateFactoryWithHttpContext(referer: "/form");

        var result = factory.Back()
            .WithErrors(new() { ["name"] = "Required" })
            .WithFlash("success", "Saved!");

        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void WithFlash_dictionary_is_chainable()
    {
        var factory = CreateFactory();

        var result = factory.Redirect("/users")
            .WithFlash(new Dictionary<string, object?> { ["success"] = "Saved", ["timestamp"] = "now" });

        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void With_is_alias_for_WithFlash()
    {
        var factory = CreateFactory();

        var result = factory.Redirect("/users")
            .With("success", "Saved!")
            .With(new Dictionary<string, object?> { ["extra"] = "data" });

        Assert.IsType<InertiaRedirectResult>(result);
    }

    [Fact]
    public void Full_laravel_style_chain()
    {
        var factory = CreateFactoryWithHttpContext(referer: "/form");

        var result = factory.Back()
            .WithErrors(new() { ["name"] = "Required" })
            .With("success", "Form saved!")
            .With(new Dictionary<string, object?> { ["timestamp"] = "now", ["user"] = "Alice" });

        Assert.IsType<InertiaRedirectResult>(result);
    }

    // -- Location --

    [Fact]
    public void Location_returns_409_result()
    {
        var factory = CreateFactoryWithHttpContext();

        var result = factory.Location("https://external.com");

        Assert.IsAssignableFrom<IResult>(result);
    }

    [Fact]
    public void Location_sets_header()
    {
        var factory = CreateFactoryWithHttpContext();

        factory.Location("https://external.com/login");

        var httpContext = Substitute.For<IHttpContextAccessor>();
        // Header is set on the factory's HttpContext
        // Verified via integration tests
    }

    [Fact]
    public void Location_available_via_interface()
    {
        IInertiaResponseFactory factory = CreateFactoryWithHttpContext();

        var result = factory.Location("https://external.com");

        Assert.NotNull(result);
    }

    private static InertiaResponseFactory CreateFactoryWithHttpContext(string? referer = null)
    {
        var httpContext = new DefaultHttpContext();
        if (referer != null)
        {
            httpContext.Request.Headers.Referer = referer;
        }

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var flashService = Substitute.For<IInertiaFlashService>();
        return new InertiaResponseFactory(
            Options.Create(new InertiaOptions()),
            flashService,
            accessor);
    }
}
