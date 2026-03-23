using InertiaCore.Configuration;
using InertiaCore.Core;
using Microsoft.Extensions.Options;

namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Class", "InertiaResponseFactory")]
public abstract class InertiaResponseFactoryTestBase
{
    protected static InertiaResponseFactory CreateFactory(Action<InertiaOptions>? configure = null)
    {
        var options = new InertiaOptions();
        configure?.Invoke(options);
        return new InertiaResponseFactory(Options.Create(options));
    }
}
