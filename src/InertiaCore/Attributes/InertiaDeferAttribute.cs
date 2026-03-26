namespace InertiaCore.Attributes;

/// <summary>
/// Marks a prop as deferred, loaded asynchronously after initial render.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaDeferAttribute : Attribute
{
    /// <summary>
    /// The deferred group name. Defaults to "default".
    /// </summary>
    public string? Group { get; set; }
}
