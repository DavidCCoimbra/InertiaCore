using InertiaCore.Context;

namespace InertiaCore.Contracts;

/// <summary>
/// An object that provides a dictionary of Inertia properties.
/// PropsResolver calls ToInertiaProperties() and merges results into the prop tree.
/// </summary>
public interface IProvidesInertiaProperties
{
    /// <summary>
    /// Returns the properties to merge into the Inertia prop tree.
    /// </summary>
    IEnumerable<KeyValuePair<string, object?>> ToInertiaProperties(RenderContext context);
}
