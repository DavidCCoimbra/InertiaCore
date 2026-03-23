using InertiaCore.Constants;
using InertiaCore.Props;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Method", "ResolveAsync")]
public class PartialReloadTests : PropsResolverTestBase
{
    [Fact]
    public async Task Only_header_filters_to_requested_props()
    {
        var resolver = CreatePartialResolver("Home/Index", only: "name,email");
        var page = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["email"] = "alice@test.com",
            ["age"] = 30,
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal(2, props.Count);
        Assert.Equal("Alice", props["name"]);
        Assert.Equal("alice@test.com", props["email"]);
        Assert.False(props.ContainsKey("age"));
    }

    [Fact]
    public async Task Except_header_excludes_specified_props()
    {
        var resolver = CreatePartialResolver("Home/Index", except: "age");
        var page = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["email"] = "alice@test.com",
            ["age"] = 30,
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal(2, props.Count);
        Assert.True(props.ContainsKey("name"));
        Assert.True(props.ContainsKey("email"));
        Assert.False(props.ContainsKey("age"));
    }

    [Fact]
    public async Task AlwaysProp_included_even_when_not_in_only()
    {
        var resolver = CreatePartialResolver("Home/Index", only: "name");
        var page = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["flash"] = new AlwaysProp("success"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal(2, props.Count);
        Assert.Equal("Alice", props["name"]);
        Assert.Equal("success", props["flash"]);
    }

    [Fact]
    public async Task Component_mismatch_returns_all_props_like_initial_load()
    {
        // Header says "Other/Component" but the response is for "Home/Index"
        var services = new ServiceCollection();
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";
        context.Request.Headers[InertiaHeaders.PartialComponent] = "Other/Component";
        context.Request.Headers[InertiaHeaders.PartialOnly] = "name";

        var resolver = new InertiaCore.Core.PropsResolver(
            services.BuildServiceProvider(), context.Request, component: "Home/Index");

        var page = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["age"] = 30,
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        // Component doesn't match, so partial filtering is disabled
        Assert.Equal(2, props.Count);
    }

    [Fact]
    public async Task Partial_reload_with_matching_component_but_no_filters_includes_all()
    {
        var resolver = CreatePartialResolver("Home/Index");
        var page = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["age"] = 30,
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal(2, props.Count);
    }

    [Fact]
    public async Task Bidirectional_prefix_match_parent_includes_children()
    {
        var resolver = CreatePartialResolver("Home/Index", only: "user");
        var page = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = "Alice",
                ["email"] = "alice@test.com",
            },
            ["posts"] = "excluded",
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.True(props.ContainsKey("user"));
        Assert.False(props.ContainsKey("posts"));
    }

    [Fact]
    public async Task Bidirectional_prefix_match_child_includes_parent()
    {
        var resolver = CreatePartialResolver("Home/Index", only: "user.name");
        var page = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = "Alice",
                ["email"] = "alice@test.com",
            },
            ["posts"] = "excluded",
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        // "user" is included because "user.name" leads to it
        Assert.True(props.ContainsKey("user"));
        Assert.False(props.ContainsKey("posts"));
    }
}
