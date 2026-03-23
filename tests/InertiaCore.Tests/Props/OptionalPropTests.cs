using InertiaCore.Contracts;
using InertiaCore.Props;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Props;

[Trait("Class", "OptionalProp")]
public class OptionalPropTests
{
    private static readonly IServiceProvider s_emptyServices = new ServiceCollection().BuildServiceProvider();

    [Fact]
    public async Task Resolves_sync_callback()
    {
        var prop = new OptionalProp(() => (object?)"optional-value");

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("optional-value", result);
    }

    [Fact]
    public async Task Resolves_async_callback()
    {
        var prop = new OptionalProp(() => Task.FromResult<object?>("async-optional"));

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("async-optional", result);
    }

    [Fact]
    public async Task Resolves_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var sp = services.BuildServiceProvider();

        var prop = new OptionalProp((IServiceProvider sp) => (object?)sp.GetRequiredService<string>());

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("injected", result);
    }

    [Fact]
    public async Task Resolves_async_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("async-injected");
        var sp = services.BuildServiceProvider();

        var prop = new OptionalProp((IServiceProvider sp) => Task.FromResult<object?>(sp.GetRequiredService<string>()));

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("async-injected", result);
    }

    [Fact]
    public void Implements_IInertiaProp()
    {
        var prop = new OptionalProp(() => (object?)null);

        Assert.IsAssignableFrom<IInertiaProp>(prop);
    }

    [Fact]
    public void Implements_IIgnoreFirstLoad()
    {
        var prop = new OptionalProp(() => (object?)null);

        Assert.IsAssignableFrom<IIgnoreFirstLoad>(prop);
    }

    [Fact]
    public void Implements_IOnceable()
    {
        var prop = new OptionalProp(() => (object?)null);

        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    [Fact]
    public void Once_not_enabled_by_default()
    {
        var prop = new OptionalProp(() => (object?)null);

        Assert.False(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void OnlyOnce_enables_once_resolution()
    {
        var prop = new OptionalProp(() => (object?)null);

        prop.OnlyOnce();

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void OnlyOnce_with_key_sets_cache_key()
    {
        var prop = new OptionalProp(() => (object?)null);

        prop.OnlyOnce("permissions");

        Assert.True(prop.Once.ShouldResolveOnce());
        Assert.Equal("permissions", prop.Once.GetKey());
    }

    [Fact]
    public void Fluent_chain_configures_once_behavior()
    {
        var prop = new OptionalProp(() => (object?)null);

        var result = prop
            .OnlyOnce()
            .As("my-key")
            .Fresh()
            .Until(TimeSpan.FromMinutes(10));

        Assert.Same(prop, result);
        Assert.True(prop.Once.ShouldResolveOnce());
        Assert.Equal("my-key", prop.Once.GetKey());
        Assert.True(prop.Once.ShouldBeRefreshed());
        Assert.NotNull(prop.Once.ExpiresAt());
    }
}
