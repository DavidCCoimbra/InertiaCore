using InertiaCore.Contracts;
using InertiaCore.Props;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Props;

[Trait("Class", "MergeProp")]
public class MergePropTests
{
    private static readonly IServiceProvider s_emptyServices = new ServiceCollection().BuildServiceProvider();

    // -- Interface implementation --

    [Fact]
    public void Implements_IInertiaProp_IMergeable_IOnceable()
    {
        var prop = new MergeProp("value");

        Assert.IsAssignableFrom<IInertiaProp>(prop);
        Assert.IsAssignableFrom<IMergeable>(prop);
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public void Does_not_implement_IIgnoreFirstLoad()
    {
        var prop = new MergeProp("value");

        Assert.False(prop is IIgnoreFirstLoad);
    }

    // -- Resolution --

    [Fact]
    public async Task Resolves_raw_value()
    {
        var prop = new MergeProp("hello");

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task Resolves_sync_callback()
    {
        var prop = new MergeProp(() => (object?)"computed");

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("computed", result);
    }

    [Fact]
    public async Task Resolves_async_callback()
    {
        var prop = new MergeProp(() => Task.FromResult<object?>("async-value"));

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("async-value", result);
    }

    [Fact]
    public async Task Resolves_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var sp = services.BuildServiceProvider();

        var prop = new MergeProp(serviceProvider => serviceProvider.GetRequiredService<string>());

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("injected", result);
    }

    [Fact]
    public async Task Resolves_async_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("async-injected");
        var sp = services.BuildServiceProvider();

        var prop = new MergeProp(serviceProvider => Task.FromResult<object?>(serviceProvider.GetRequiredService<string>()));

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("async-injected", result);
    }

    // -- Merge behavior --

    [Fact]
    public void Merge_enabled_by_default()
    {
        var prop = new MergeProp("value");

        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Appends_at_root_by_default()
    {
        var prop = new MergeProp("value");

        Assert.True(prop.Merge.AppendsAtRoot());
    }

    [Fact]
    public void WithDeepMerge_enables_deep_merge()
    {
        var prop = new MergeProp("value");

        var result = prop.WithDeepMerge();

        Assert.Same(prop, result);
        Assert.True(prop.Merge.ShouldDeepMerge());
    }

    [Fact]
    public void Append_with_path()
    {
        var prop = new MergeProp("value");

        prop.Append("data.items", "id");

        Assert.Equal(["data.items"], prop.Merge.GetAppendsAtPaths());
        Assert.Equal(["data.items.id"], prop.Merge.MatchesOn());
    }

    [Fact]
    public void Prepend_with_path()
    {
        var prop = new MergeProp("value");

        prop.Prepend("data.items");

        Assert.Equal(["data.items"], prop.Merge.GetPrependsAtPaths());
    }

    // -- Once fluent API --

    [Fact]
    public void Once_not_enabled_by_default()
    {
        var prop = new MergeProp("value");

        Assert.False(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void OnlyOnce_enables_once()
    {
        var prop = new MergeProp("value");

        prop.OnlyOnce();

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void Fluent_chain_configures_merge_and_once()
    {
        var prop = new MergeProp("value")
            .WithDeepMerge()
            .OnlyOnce("merge-data")
            .As("my-key")
            .Fresh()
            .Until(TimeSpan.FromMinutes(10));

        Assert.True(prop.Merge.ShouldDeepMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
        Assert.Equal("my-key", prop.Once.GetKey());
        Assert.True(prop.Once.ShouldBeRefreshed());
        Assert.NotNull(prop.Once.ExpiresAt());
    }
}
