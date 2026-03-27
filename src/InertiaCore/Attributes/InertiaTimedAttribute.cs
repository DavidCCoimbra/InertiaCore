namespace InertiaCore.Attributes;

/// <summary>
/// Marks a prop for server-driven refresh at the specified interval.
/// The client polls for fresh data automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaTimedAttribute : Attribute
{
    /// <summary>
    /// Refresh interval in seconds.
    /// </summary>
    public int IntervalSeconds { get; set; }
}
