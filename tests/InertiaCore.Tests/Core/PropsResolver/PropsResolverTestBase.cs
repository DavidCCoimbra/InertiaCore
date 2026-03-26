using InertiaCore.Constants;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
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

    protected static InertiaCore.Core.PropsResolver CreatePartialResolver(
        string component,
        string? only = null,
        string? except = null,
        string? reset = null,
        string? loadedOnceProps = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);

        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Inertia] = "true";
        context.Request.Headers[InertiaHeaders.PartialComponent] = component;

        if (only != null)
        {
            context.Request.Headers[InertiaHeaders.PartialData] = only;
        }

        if (except != null)
        {
            context.Request.Headers[InertiaHeaders.PartialExcept] = except;
        }

        if (reset != null)
        {
            context.Request.Headers[InertiaHeaders.Reset] = reset;
        }

        if (loadedOnceProps != null)
        {
            context.Request.Headers[InertiaHeaders.ExceptOnceProps] = loadedOnceProps;
        }

        return new InertiaCore.Core.PropsResolver(services.BuildServiceProvider(), context.Request, component);
    }
}
