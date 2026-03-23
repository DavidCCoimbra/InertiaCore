using InertiaCore.Props;

namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Method", "ResolveAsync")]
public class PropTypeDetectionTests : PropsResolverTestBase
{
    [Fact]
    public async Task Resolves_IInertiaProp_via_ResolveAsync()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["greeting"] = new AlwaysProp("hello"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("hello", props["greeting"]);
    }

    [Fact]
    public async Task IIgnoreFirstLoad_excluded_on_initial_load()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["visible"] = "yes",
            ["optional"] = new OptionalProp(() => (object?)"lazy"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.True(props.ContainsKey("visible"));
        Assert.False(props.ContainsKey("optional"));
    }

    [Fact]
    public async Task IIgnoreFirstLoad_included_on_partial_reload()
    {
        var resolver = CreatePartialResolver("Home/Index", only: "optional");
        var page = new Dictionary<string, object?>
        {
            ["optional"] = new OptionalProp(() => (object?)"lazy-value"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("lazy-value", props["optional"]);
    }

    [Fact]
    public async Task DeferProp_excluded_on_initial_load()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["visible"] = "yes",
            ["deferred"] = new DeferProp(() => (object?)"heavy-data"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.True(props.ContainsKey("visible"));
        Assert.False(props.ContainsKey("deferred"));
    }

    [Fact]
    public async Task DeferProp_included_on_partial_reload()
    {
        var resolver = CreatePartialResolver("Home/Index", only: "deferred");
        var page = new Dictionary<string, object?>
        {
            ["deferred"] = new DeferProp(() => (object?)"heavy-data"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("heavy-data", props["deferred"]);
    }

    [Fact]
    public async Task OnceProp_excluded_on_initial_load()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["visible"] = "yes",
            ["permissions"] = new OnceProp(() => (object?)"admin"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.True(props.ContainsKey("visible"));
        Assert.False(props.ContainsKey("permissions"));
    }

    [Fact]
    public async Task MergeProp_included_on_initial_load()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["items"] = new MergeProp(new[] { 1, 2, 3 }),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.True(props.ContainsKey("items"));
    }

    [Fact]
    public async Task Closure_returning_IInertiaProp_is_resolved()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["computed"] = (Func<object?>)(() => new AlwaysProp("nested-value")),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("nested-value", props["computed"]);
    }

    [Fact]
    public async Task Closure_returning_dictionary_is_recursed()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["computed"] = (Func<object?>)(() => new Dictionary<string, object?>
            {
                ["nested"] = "value",
            }),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        var dict = Assert.IsType<Dictionary<string, object?>>(props["computed"]);
        Assert.Equal("value", dict["nested"]);
    }

    [Fact]
    public async Task Closure_returning_IIgnoreFirstLoad_is_excluded_on_initial()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["lazy"] = (Func<object?>)(() => new OptionalProp(() => (object?)"should-not-appear")),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Null(props["lazy"]);
    }
}
