namespace InertiaCore.Attributes;

/// <summary>
/// Marks a prop to be included in the initial HTML page data when async page data is enabled.
/// Props without this attribute are delivered via a parallel HTTP fetch.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaPageDataAttribute : Attribute;
