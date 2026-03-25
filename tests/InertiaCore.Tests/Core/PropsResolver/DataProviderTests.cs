using InertiaCore.Constants;
using InertiaCore.Context;
using InertiaCore.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Method", "ResolveAsync")]
public class DataProviderTests : PropsResolverTestBase
{
    [Fact]
    public async Task Resolves_IProvidesInertiaProperties()
    {
        var resolver = CreatePartialResolver("Home/Index");
        var provider = new TestPropertiesProvider(new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["role"] = "admin",
        });

        var page = new Dictionary<string, object?>
        {
            ["_provider"] = provider,
            ["other"] = "value",
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("Alice", props["name"]);
        Assert.Equal("admin", props["role"]);
        Assert.Equal("value", props["other"]);
        Assert.False(props.ContainsKey("_provider"));
    }

    [Fact]
    public async Task Resolves_IProvidesInertiaProperty()
    {
        var resolver = CreatePartialResolver("Home/Index");
        var provider = new TestPropertyProvider("resolved-value");

        var page = new Dictionary<string, object?>
        {
            ["computed"] = provider,
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("resolved-value", props["computed"]);
    }

    [Fact]
    public async Task Provider_without_httpcontext_is_not_resolved()
    {
        var resolver = CreateResolver();
        var provider = new TestPropertyProvider("value");

        var page = new Dictionary<string, object?>
        {
            ["computed"] = provider,
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        // Without HttpContext, IProvidesInertiaProperty falls through to default (returns the object)
        Assert.IsType<TestPropertyProvider>(props["computed"]);
    }

    [Fact]
    public async Task Properties_provider_receives_render_context()
    {
        var resolver = CreatePartialResolver("Dashboard/Index");
        string? capturedComponent = null;
        var provider = new CallbackPropertiesProvider(ctx =>
        {
            capturedComponent = ctx.Component;
            return [new("fromProvider", "yes")];
        });

        var page = new Dictionary<string, object?>
        {
            ["_provider"] = provider,
        };

        await resolver.ResolveAsync(new(), page);

        Assert.Equal("Dashboard/Index", capturedComponent);
    }

    private class TestPropertiesProvider(Dictionary<string, object?> props) : IProvidesInertiaProperties
    {
        public IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context) => props;
    }

    private class CallbackPropertiesProvider(
        Func<RenderContext, IEnumerable<KeyValuePair<string, object?>>> callback) : IProvidesInertiaProperties
    {
        public IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context) =>
            callback(context);
    }

    private class TestPropertyProvider(object? value) : IProvidesInertiaProperty
    {
        public object? ToInertiaProperty(PropertyContext context) => value;
    }
}
