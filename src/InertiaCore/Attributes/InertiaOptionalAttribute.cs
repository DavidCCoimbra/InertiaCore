namespace InertiaCore.Attributes;

/// <summary>
/// Marks a prop as excluded from the initial load, included when explicitly requested.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaOptionalAttribute : Attribute;
