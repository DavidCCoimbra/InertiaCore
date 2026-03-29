using InertiaCore.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Filters;

/// <summary>
/// Endpoint filter that applies Inertia configuration (shared props, version, etc.)
/// to specific routes or route groups.
/// </summary>
public sealed class InertiaEndpointFilter : IEndpointFilter
{
    private readonly Action<IInertiaResponseFactory> _configure;

    /// <summary>
    /// Creates a filter that applies the given configuration to the Inertia factory.
    /// </summary>
    public InertiaEndpointFilter(Action<IInertiaResponseFactory> configure)
    {
        _configure = configure;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<IInertiaResponseFactory>();
        _configure(factory);

        return await next(context);
    }
}
