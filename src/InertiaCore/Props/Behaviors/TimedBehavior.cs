namespace InertiaCore.Props.Behaviors;

/// <summary>
/// Configuration for server-driven prop refresh intervals.
/// </summary>
public sealed class TimedBehavior
{
    private int? _intervalMs;

    /// <summary>
    /// Whether timed refresh is enabled.
    /// </summary>
    public bool IsTimed() => _intervalMs.HasValue;

    /// <summary>
    /// The refresh interval in milliseconds.
    /// </summary>
    public int? IntervalMs() => _intervalMs;

    /// <summary>
    /// Sets the refresh interval.
    /// </summary>
    public void SetInterval(TimeSpan interval)
    {
        _intervalMs = (int)interval.TotalMilliseconds;
    }
}
