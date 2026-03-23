using InertiaCore.Core;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Class", "PropsResolver")]
public abstract class PropsResolverTestBase
{
    protected static InertiaCore.Core.PropsResolver CreateResolver(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        return new InertiaCore.Core.PropsResolver(services.BuildServiceProvider());
    }
}
