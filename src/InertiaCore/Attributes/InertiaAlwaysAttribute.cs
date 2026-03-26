namespace InertiaCore.Attributes;

/// <summary>
/// Marks a prop as always included, even during partial reloads.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaAlwaysAttribute : Attribute;
