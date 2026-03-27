namespace InertiaCore.Props.Behaviors;

/// <summary>
/// Configuration for fallback values when prop resolution fails.
/// </summary>
public sealed class FallbackBehavior
{
    private bool _hasFallback;
    private object? _fallbackValue;

    /// <summary>
    /// Whether a fallback value is configured.
    /// </summary>
    public bool HasFallback() => _hasFallback;

    /// <summary>
    /// The fallback value to use when resolution fails.
    /// </summary>
    public object? GetFallback() => _fallbackValue;

    /// <summary>
    /// Sets the fallback value.
    /// </summary>
    public void SetFallback(object? value)
    {
        _hasFallback = true;
        _fallbackValue = value;
    }
}
