using InertiaCore.Contracts;
using Microsoft.AspNetCore.Http;

namespace InertiaCore.Core;

/// <summary>
/// An ISharedPropsProvider backed by a typed factory delegate.
/// Supports Inertia attributes on the props type for declarative behavior.
/// </summary>
internal sealed class TypedSharedPropsProvider<TProps>(Func<HttpContext, TProps> factory) : ISharedPropsProvider
    where TProps : class
{
    public Dictionary<string, object?> GetSharedProps(HttpContext context) =>
        PropAttributeResolver.ConvertToPropsDict(factory(context));
}
