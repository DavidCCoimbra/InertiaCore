using InertiaCore.Configuration;
using InertiaCore.Core;
using InertiaCore.Extensions;
using InertiaCore.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InertiaCore.Tests.Extensions;

[Trait("Class", "ServiceCollectionExtensions")]
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Registers_PageDataCache_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddInertia();

        var descriptor = services.Single(s => s.ServiceType == typeof(IPageDataCache));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Registers_InertiaResponseFactory_as_scoped()
    {
        var services = new ServiceCollection();

        services.AddInertia();

        var descriptor = services.Single(s => s.ServiceType == typeof(IInertiaResponseFactory));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Registers_InertiaMiddleware_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddInertia();

        var descriptor = services.Single(s => s.ServiceType == typeof(InertiaMiddleware));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Registers_InertiaOptions_with_defaults()
    {
        var services = new ServiceCollection();
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<InertiaOptions>>().Value;

        Assert.Equal("App", options.RootView);
        Assert.Null(options.Version);
        Assert.False(options.EncryptHistory);
    }

    [Fact]
    public void Applies_configuration_lambda()
    {
        var services = new ServiceCollection();
        services.AddInertia(opt =>
        {
            opt.RootView = "Layout";
            opt.Version = "v2";
            opt.EncryptHistory = true;
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<InertiaOptions>>().Value;

        Assert.Equal("Layout", options.RootView);
        Assert.Equal("v2", options.Version);
        Assert.True(options.EncryptHistory);
    }

    [Fact]
    public void Registers_IInertiaFlashService_as_scoped()
    {
        var services = new ServiceCollection();

        services.AddInertia();

        var descriptor = services.Single(s => s.ServiceType == typeof(IInertiaFlashService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Registers_IInertiaErrorService_as_scoped()
    {
        var services = new ServiceCollection();

        services.AddInertia();

        var descriptor = services.Single(s => s.ServiceType == typeof(IInertiaErrorService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Registers_EncryptHistoryMiddleware_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddInertia();

        var descriptor = services.Single(s => s.ServiceType == typeof(EncryptHistoryMiddleware));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Returns_service_collection_for_chaining()
    {
        var services = new ServiceCollection();

        var result = services.AddInertia();

        Assert.Same(services, result);
    }

    [Fact]
    public void ResponseFactory_is_scoped_per_request()
    {
        var services = new ServiceCollection();
        services.AddInertia();
        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var factory1 = scope1.ServiceProvider.GetRequiredService<IInertiaResponseFactory>();
        var factory2 = scope2.ServiceProvider.GetRequiredService<IInertiaResponseFactory>();

        Assert.NotSame(factory1, factory2);
    }
}
