namespace InertiaCore.Core;

/// <summary>
/// Sentinel value indicating a prop should be excluded from the response.
/// Returned by Inertia.When() when the condition is false.
/// </summary>
public sealed class ConditionalProp
{
    /// <summary>
    /// Singleton instance representing an excluded prop.
    /// </summary>
    public static readonly ConditionalProp Excluded = new();

    private ConditionalProp() { }
}
