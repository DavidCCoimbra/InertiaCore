namespace InertiaCore.Attributes;

/// <summary>
/// Specifies a fallback value type for when prop resolution fails.
/// The type must have a parameterless constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaFallbackAttribute : Attribute
{
    /// <summary>
    /// The type that provides the fallback value. Must have a parameterless constructor.
    /// The instance is used directly as the fallback value.
    /// </summary>
    public Type FallbackType { get; }

    /// <summary>
    /// Creates a fallback attribute with the specified provider type.
    /// </summary>
    public InertiaFallbackAttribute(Type fallbackType)
    {
        FallbackType = fallbackType;
    }
}
