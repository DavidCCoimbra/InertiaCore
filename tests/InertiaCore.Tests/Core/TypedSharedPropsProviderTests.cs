using InertiaCore.Attributes;
using InertiaCore.Contracts;
using InertiaCore.Core;
using InertiaCore.Props;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace InertiaCore.Tests.Core;

[Trait("Class", "TypedSharedPropsProvider")]
public class TypedSharedPropsProviderTests
{
    [Fact]
    public void Converts_typed_props_to_dictionary()
    {
        var provider = new TypedSharedPropsProvider<SharedProps>(
            _ => new SharedProps { AppName = "MyApp", Locale = "en" });
        var context = new DefaultHttpContext();

        var result = provider.GetSharedProps(context);

        Assert.Equal("MyApp", result["AppName"]);
        Assert.Equal("en", result["Locale"]);
    }

    [Fact]
    public void Respects_inertia_attributes_on_shared_props()
    {
        var provider = new TypedSharedPropsProvider<AttributedSharedProps>(
            _ => new AttributedSharedProps
            {
                AppName = "MyApp",
                Permissions = new[] { "read", "write" },
            });
        var context = new DefaultHttpContext();

        var result = provider.GetSharedProps(context);

        Assert.IsType<AlwaysProp>(result["AppName"]);
        Assert.IsType<OnceProp>(result["Permissions"]);
    }

    [Fact]
    public void Receives_http_context()
    {
        HttpContext? captured = null;
        var provider = new TypedSharedPropsProvider<SharedProps>(ctx =>
        {
            captured = ctx;
            return new SharedProps { AppName = "Test" };
        });
        var context = new DefaultHttpContext();

        provider.GetSharedProps(context);

        Assert.Same(context, captured);
    }

    [Fact]
    public void Implements_ISharedPropsProvider()
    {
        var provider = new TypedSharedPropsProvider<SharedProps>(
            _ => new SharedProps { AppName = "Test" });

        Assert.IsAssignableFrom<ISharedPropsProvider>(provider);
    }

    [Fact]
    public void Handles_null_property_values()
    {
        var provider = new TypedSharedPropsProvider<SharedProps>(
            _ => new SharedProps { AppName = null, Locale = null });
        var context = new DefaultHttpContext();

        var result = provider.GetSharedProps(context);

        Assert.Null(result["AppName"]);
        Assert.Null(result["Locale"]);
    }

    private class SharedProps
    {
        public string? AppName { get; init; }
        public string? Locale { get; init; }
    }

    private class AttributedSharedProps
    {
        [InertiaAlways]
        public string? AppName { get; init; }

        [InertiaOnce]
        public string[]? Permissions { get; init; }
    }
}
