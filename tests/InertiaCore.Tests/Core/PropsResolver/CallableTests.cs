using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Method", "ResolveAsync")]
public class CallableTests : PropsResolverTestBase
{
    [Fact]
    public async Task Resolves_sync_func()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["computed"] = (Func<object?>)(() => "resolved"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("resolved", props["computed"]);
    }

    [Fact]
    public async Task Resolves_async_func()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["async"] = (Func<Task<object?>>)(() => Task.FromResult<object?>("async-value")),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("async-value", props["async"]);
    }

    [Fact]
    public async Task Resolves_service_provider_func()
    {
        var resolver = CreateResolver(services =>
            services.AddSingleton("injected-value"));

        var page = new Dictionary<string, object?>
        {
            ["fromDI"] = (Func<IServiceProvider, object?>)(sp => sp.GetRequiredService<string>()),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("injected-value", props["fromDI"]);
    }

    [Fact]
    public async Task Resolves_async_service_provider_func()
    {
        var resolver = CreateResolver(services =>
            services.AddSingleton("async-injected"));

        var page = new Dictionary<string, object?>
        {
            ["asyncDI"] = (Func<IServiceProvider, Task<object?>>)(sp =>
                Task.FromResult<object?>(sp.GetRequiredService<string>())),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("async-injected", props["asyncDI"]);
    }

    [Fact]
    public async Task Func_returning_null_preserves_null()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["nullFunc"] = (Func<object?>)(() => null),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.True(props.ContainsKey("nullFunc"));
        Assert.Null(props["nullFunc"]);
    }

    [Fact]
    public async Task Plain_values_pass_through()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["string"] = "hello",
            ["int"] = 42,
            ["bool"] = true,
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("hello", props["string"]);
        Assert.Equal(42, props["int"]);
        Assert.Equal(true, props["bool"]);
    }
}
