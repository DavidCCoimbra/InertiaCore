using InertiaCore.Contracts;
using InertiaCore.Props;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Props;

[Trait("Class", "DeferProp")]
public class DeferPropTests
{
    private static readonly IServiceProvider s_emptyServices = new ServiceCollection().BuildServiceProvider();

    // -- Interface implementation --

    [Fact]
    public void Implements_all_contracts()
    {
        var prop = new DeferProp(() => (object?)null);

        Assert.IsAssignableFrom<IInertiaProp>(prop);
        Assert.IsAssignableFrom<IIgnoreFirstLoad>(prop);
        Assert.IsAssignableFrom<IDeferrable>(prop);
        Assert.IsAssignableFrom<IMergeable>(prop);
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    // -- Resolution --

    [Fact]
    public async Task Resolves_sync_callback()
    {
        var prop = new DeferProp(() => (object?)"deferred");

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("deferred", result);
    }

    [Fact]
    public async Task Resolves_async_callback()
    {
        var prop = new DeferProp(() => Task.FromResult<object?>("async-deferred"));

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("async-deferred", result);
    }

    [Fact]
    public async Task Resolves_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var sp = services.BuildServiceProvider();

        var prop = new DeferProp(serviceProvider => serviceProvider.GetRequiredService<string>());

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("injected", result);
    }

    [Fact]
    public async Task Resolves_async_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("async-injected");
        var sp = services.BuildServiceProvider();

        var prop = new DeferProp((IServiceProvider sp) => Task.FromResult<object?>(sp.GetRequiredService<string>()));

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("async-injected", result);
    }

    // -- Defer behavior --

    [Fact]
    public void Deferred_by_default()
    {
        var prop = new DeferProp(() => (object?)null);

        Assert.True(prop.Defer.ShouldDefer());
    }

    [Fact]
    public void Uses_default_group_when_none_specified()
    {
        var prop = new DeferProp(() => (object?)null);

        Assert.Equal("default", prop.Defer.Group());
    }

    [Fact]
    public void Uses_custom_group()
    {
        var prop = new DeferProp(() => (object?)null, group: "charts");

        Assert.Equal("charts", prop.Defer.Group());
    }

    // -- Merge fluent API --

    [Fact]
    public void Merge_not_enabled_by_default()
    {
        var prop = new DeferProp(() => (object?)null);

        Assert.False(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void WithMerge_enables_merge()
    {
        var prop = new DeferProp(() => (object?)null);

        var result = prop.WithMerge();

        Assert.Same(prop, result);
        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void WithDeepMerge_enables_deep_merge()
    {
        var prop = new DeferProp(() => (object?)null);

        prop.WithDeepMerge();

        Assert.True(prop.Merge.ShouldDeepMerge());
    }

    [Fact]
    public void Append_configures_merge_path()
    {
        var prop = new DeferProp(() => (object?)null);

        prop.Append("data.items", "id");

        Assert.True(prop.Merge.ShouldMerge());
        Assert.Equal(["data.items"], prop.Merge.GetAppendsAtPaths());
        Assert.Equal(["data.items.id"], prop.Merge.MatchesOn());
    }

    [Fact]
    public void Prepend_configures_merge_path()
    {
        var prop = new DeferProp(() => (object?)null);

        prop.Prepend("data.items");

        Assert.True(prop.Merge.ShouldMerge());
        Assert.Equal(["data.items"], prop.Merge.GetPrependsAtPaths());
    }

    // -- Once fluent API --

    [Fact]
    public void Once_not_enabled_by_default()
    {
        var prop = new DeferProp(() => (object?)null);

        Assert.False(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void OnlyOnce_enables_once_resolution()
    {
        var prop = new DeferProp(() => (object?)null);

        prop.OnlyOnce();

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void As_sets_custom_cache_key()
    {
        var prop = new DeferProp(() => (object?)null);

        prop.As("my-key");

        Assert.Equal("my-key", prop.Once.GetKey());
    }

    [Fact]
    public void Fluent_chain_configures_all_behaviors()
    {
        var prop = new DeferProp(() => (object?)null, group: "analytics")
            .WithMerge()
            .OnlyOnce("analytics-data")
            .Fresh()
            .Until(TimeSpan.FromMinutes(5));

        Assert.Equal("analytics", prop.Defer.Group());
        Assert.True(prop.Merge.ShouldMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
        Assert.Equal("analytics-data", prop.Once.GetKey());
        Assert.True(prop.Once.ShouldBeRefreshed());
        Assert.NotNull(prop.Once.ExpiresAt());
    }
}
