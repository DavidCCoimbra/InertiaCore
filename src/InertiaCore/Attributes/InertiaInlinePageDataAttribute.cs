namespace InertiaCore.Attributes;

/// <summary>
/// Forces all props to be inlined in the HTML page data, bypassing async page data for this endpoint.
/// Can be applied to a controller action or an entire controller.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class InertiaInlinePageDataAttribute : Attribute;
