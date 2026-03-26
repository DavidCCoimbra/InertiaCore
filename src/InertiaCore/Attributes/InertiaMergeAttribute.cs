namespace InertiaCore.Attributes;

/// <summary>
/// Marks a prop as mergeable with existing client-side data.
/// Default behavior is shallow append. Set Deep, or Prepend to change the strategy.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaMergeAttribute : Attribute
{
    /// <summary>
    /// Enables deep (recursive) merge instead of shallow merge.
    /// </summary>
    public bool Deep { get; set; }

    /// <summary>
    /// Prepends instead of appending. Only applies to shallow merge.
    /// </summary>
    public bool Prepend { get; set; }
}
