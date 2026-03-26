using InertiaCore.Contracts;
using InertiaCore.Props;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Props;

[Trait("Class", "OnceProp")]
public class OncePropTests
{
    private static readonly IServiceProvider s_emptyServices = new ServiceCollection().BuildServiceProvider();

    // -- Interface implementation --

    [Fact]
    public void Implements_IInertiaProp_IIgnoreFirstLoad_IOnceable()
    {
        var prop = new OnceProp(() => (object?)null);

        Assert.IsAssignableFrom<IInertiaProp>(prop);
        Assert.IsAssignableFrom<IIgnoreFirstLoad>(prop);
        Assert.IsAssignableFrom<IOnceable>(prop);
    }

    // -- Resolution --

    [Fact]
    public async Task Resolves_sync_callback()
    {
        var prop = new OnceProp(() => (object?)"once-value");

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("once-value", result);
    }

    [Fact]
    public async Task Resolves_async_callback()
    {
        var prop = new OnceProp(() => Task.FromResult<object?>("async-once"));

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("async-once", result);
    }

    [Fact]
    public async Task Resolves_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var sp = services.BuildServiceProvider();

        var prop = new OnceProp(serviceProvider => serviceProvider.GetRequiredService<string>());

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("injected", result);
    }

    [Fact]
    public async Task Resolves_async_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("async-injected");
        var sp = services.BuildServiceProvider();

        var prop = new OnceProp(serviceProvider => Task.FromResult<object?>(serviceProvider.GetRequiredService<string>()));

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("async-injected", result);
    }

    // -- Once behavior --

    [Fact]
    public void Once_enabled_by_default()
    {
        var prop = new OnceProp(() => (object?)null);

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void As_sets_cache_key()
    {
        var prop = new OnceProp(() => (object?)null);

        prop.As("permissions");

        Assert.Equal("permissions", prop.Once.GetKey());
    }

    [Fact]
    public void Fresh_enables_refresh()
    {
        var prop = new OnceProp(() => (object?)null);

        prop.Fresh();

        Assert.True(prop.Once.ShouldBeRefreshed());
    }

    [Fact]
    public void Until_sets_ttl()
    {
        var prop = new OnceProp(() => (object?)null);

        prop.Until(TimeSpan.FromHours(1));

        Assert.NotNull(prop.Once.ExpiresAt());
    }

    [Fact]
    public void Fluent_chain_configures_once()
    {
        var prop = new OnceProp(() => (object?)null)
            .As("user-roles")
            .Fresh()
            .Until(TimeSpan.FromMinutes(30));

        Assert.Equal("user-roles", prop.Once.GetKey());
        Assert.True(prop.Once.ShouldBeRefreshed());
        Assert.NotNull(prop.Once.ExpiresAt());
    }
}
