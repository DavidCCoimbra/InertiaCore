using InertiaCore.Contracts;
using InertiaCore.Props;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Props;

[Trait("Class", "GenericPropTypes")]
public class GenericPropTypeTests
{
    private static readonly IServiceProvider s_emptyServices = new ServiceCollection().BuildServiceProvider();

    // -- AlwaysProp<T> --

    [Fact]
    public async Task AlwaysProp_T_resolves_value()
    {
        var prop = new AlwaysProp<string>("hello");
        Assert.Equal("hello", await prop.ResolveAsync(s_emptyServices));
    }

    [Fact]
    public async Task AlwaysProp_T_resolves_callback()
    {
        var prop = new AlwaysProp<int>(() => 42);
        Assert.Equal(42, await prop.ResolveAsync(s_emptyServices));
    }

    [Fact]
    public void AlwaysProp_T_implements_IInertiaProp()
    {
        Assert.IsAssignableFrom<IInertiaProp>(new AlwaysProp<string>("test"));
    }

    // -- OptionalProp<T> --

    [Fact]
    public async Task OptionalProp_T_resolves_callback()
    {
        var prop = new OptionalProp<string>(() => "optional");
        Assert.Equal("optional", await prop.ResolveAsync(s_emptyServices));
    }

    [Fact]
    public void OptionalProp_T_implements_contracts()
    {
        var prop = new OptionalProp<string>(() => "test");
        Assert.IsAssignableFrom<IInertiaProp>(prop);
        Assert.IsAssignableFrom<IIgnoreFirstLoad>(prop);
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public void OptionalProp_T_fluent_once()
    {
        var prop = new OptionalProp<string>(() => "test")
            .OnlyOnce("key")
            .Fresh()
            .Until(TimeSpan.FromMinutes(5));

        Assert.True(prop.Once.ShouldResolveOnce());
        Assert.Equal("key", prop.Once.GetKey());
        Assert.True(prop.Once.ShouldBeRefreshed());
    }

    // -- DeferProp<T> --

    [Fact]
    public async Task DeferProp_T_resolves_callback()
    {
        var prop = new DeferProp<string>(() => "deferred");
        Assert.Equal("deferred", await prop.ResolveAsync(s_emptyServices));
    }

    [Fact]
    public void DeferProp_T_implements_all_contracts()
    {
        var prop = new DeferProp<string>(() => "test");
        Assert.IsAssignableFrom<IInertiaProp>(prop);
        Assert.IsAssignableFrom<IIgnoreFirstLoad>(prop);
        Assert.IsAssignableFrom<IDeferrable>(prop);
        Assert.IsAssignableFrom<IMergeable>(prop);
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public void DeferProp_T_with_group()
    {
        var prop = new DeferProp<int>(() => 42, group: "analytics");
        Assert.Equal("analytics", prop.Defer.Group());
    }

    [Fact]
    public void DeferProp_T_fluent_chain()
    {
        var prop = new DeferProp<string>(() => "test")
            .WithMerge()
            .OnlyOnce("key");

        Assert.True(prop.Merge.ShouldMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    // -- MergeProp<T> --

    [Fact]
    public async Task MergeProp_T_resolves_value()
    {
        var prop = new MergeProp<int[]>([1, 2, 3]);
        var result = await prop.ResolveAsync(s_emptyServices);
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public async Task MergeProp_T_resolves_callback()
    {
        var prop = new MergeProp<string>(() => "merged");
        Assert.Equal("merged", await prop.ResolveAsync(s_emptyServices));
    }

    [Fact]
    public void MergeProp_T_merge_enabled_by_default()
    {
        var prop = new MergeProp<int[]>([1]);
        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void MergeProp_T_fluent_chain()
    {
        var prop = new MergeProp<int[]>([1])
            .WithDeepMerge()
            .OnlyOnce();

        Assert.True(prop.Merge.ShouldDeepMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    // -- OnceProp<T> --

    [Fact]
    public async Task OnceProp_T_resolves_callback()
    {
        var prop = new OnceProp<string>(() => "once");
        Assert.Equal("once", await prop.ResolveAsync(s_emptyServices));
    }

    [Fact]
    public void OnceProp_T_once_enabled_by_default()
    {
        var prop = new OnceProp<string>(() => "test");
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void OnceProp_T_fluent_chain()
    {
        var prop = new OnceProp<string>(() => "test")
            .As("my-key")
            .Fresh()
            .Until(TimeSpan.FromMinutes(10));

        Assert.Equal("my-key", prop.Once.GetKey());
        Assert.True(prop.Once.ShouldBeRefreshed());
    }

    // -- Typed props in dictionary (how PropsResolver sees them) --

    [Fact]
    public async Task Generic_props_work_through_resolver()
    {
        var resolver = new InertiaCore.Core.PropsResolver(s_emptyServices);
        var page = new Dictionary<string, object?>
        {
            ["name"] = new AlwaysProp<string>("Alice"),
            ["items"] = new MergeProp<int[]>([1, 2, 3]),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("Alice", props["name"]);
        Assert.Equal(new[] { 1, 2, 3 }, props["items"]);
    }
}
