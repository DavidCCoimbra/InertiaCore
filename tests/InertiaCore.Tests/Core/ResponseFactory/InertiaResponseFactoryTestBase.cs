using InertiaCore.Configuration;
using InertiaCore.Core;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Class", "InertiaResponseFactory")]
public abstract class InertiaResponseFactoryTestBase
{
    protected static InertiaResponseFactory CreateFactory(Action<InertiaOptions>? configure = null)
    {
        var options = new InertiaOptions();
        configure?.Invoke(options);
        var flashService = Substitute.For<IInertiaFlashService>();
        return new InertiaResponseFactory(Options.Create(options), flashService);
    }
}
