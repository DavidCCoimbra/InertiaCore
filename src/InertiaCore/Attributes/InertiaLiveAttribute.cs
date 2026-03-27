namespace InertiaCore.Attributes;

/// <summary>
/// Marks a prop as receiving real-time updates via SignalR.
/// When the channel fires, connected clients auto-reload this prop.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaLiveAttribute : Attribute
{
    /// <summary>
    /// The SignalR channel name. If null, uses the prop name as the channel.
    /// </summary>
    public string? Channel { get; set; }
}
