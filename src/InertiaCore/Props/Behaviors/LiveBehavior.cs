namespace InertiaCore.Props.Behaviors;

/// <summary>
/// Configuration for real-time prop updates via SignalR.
/// </summary>
public sealed class LiveBehavior
{
    private bool _live;
    private string? _channel;

    /// <summary>
    /// Whether live updates are enabled for this prop.
    /// </summary>
    public bool IsLive() => _live;

    /// <summary>
    /// The SignalR channel this prop listens on.
    /// </summary>
    public string? Channel() => _channel;

    /// <summary>
    /// Enables live updates on the specified channel.
    /// </summary>
    public void Enable(string? channel = null)
    {
        _live = true;
        _channel = channel;
    }
}
