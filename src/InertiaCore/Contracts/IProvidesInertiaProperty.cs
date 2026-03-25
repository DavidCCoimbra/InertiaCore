using InertiaCore.Context;

namespace InertiaCore.Contracts;

/// <summary>
/// An object that provides a single Inertia property value.
/// When a prop value implements this, PropsResolver calls ToInertiaProperty()
/// instead of serializing the object directly.
/// </summary>
public interface IProvidesInertiaProperty
{
    /// <summary>
    /// Returns the resolved property value.
    /// </summary>
    object? ToInertiaProperty(PropertyContext context);
}
