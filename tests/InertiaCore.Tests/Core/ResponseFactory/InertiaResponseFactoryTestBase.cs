using InertiaCore.Configuration;
using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
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
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        return new InertiaResponseFactory(Options.Create(options), flashService, httpContextAccessor);
    }
}
