using InertiaCore.Contracts;
using InertiaCore.Props;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Props;

[Trait("Class", "AlwaysProp")]
public class AlwaysPropTests
{
    private static readonly IServiceProvider s_emptyServices = new ServiceCollection().BuildServiceProvider();

    [Fact]
    public async Task Resolves_raw_value()
    {
        var prop = new AlwaysProp("hello");

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task Resolves_null_value()
    {
        var prop = new AlwaysProp((object?)null);

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Null(result);
    }

    [Fact]
    public async Task Resolves_sync_callback()
    {
        var prop = new AlwaysProp(() => (object?)"computed");

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("computed", result);
    }

    [Fact]
    public async Task Resolves_async_callback()
    {
        var prop = new AlwaysProp(() => Task.FromResult<object?>("async-value"));

        var result = await prop.ResolveAsync(s_emptyServices);

        Assert.Equal("async-value", result);
    }

    [Fact]
    public async Task Resolves_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var sp = services.BuildServiceProvider();

        var prop = new AlwaysProp((IServiceProvider sp) => (object?)sp.GetRequiredService<string>());

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("injected", result);
    }

    [Fact]
    public async Task Resolves_async_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("async-injected");
        var sp = services.BuildServiceProvider();

        var prop = new AlwaysProp((IServiceProvider sp) => Task.FromResult<object?>(sp.GetRequiredService<string>()));

        var result = await prop.ResolveAsync(sp);

        Assert.Equal("async-injected", result);
    }

    [Fact]
    public void Implements_IInertiaProp()
    {
        var prop = new AlwaysProp("test");

        Assert.IsAssignableFrom<IInertiaProp>(prop);
    }

    [Fact]
    public void Does_not_implement_IIgnoreFirstLoad()
    {
        var prop = new AlwaysProp("test");

        Assert.False(prop is IIgnoreFirstLoad);
    }
}
