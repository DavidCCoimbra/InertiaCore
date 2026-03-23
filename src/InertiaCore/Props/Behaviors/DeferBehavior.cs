namespace InertiaCore.Props.Behaviors;

/// <summary>
/// Configuration for deferred prop loading, including group and merge strategy.
/// </summary>
public class DeferBehavior
{
    private bool _deferred;
    private string? _group;

    /// <summary>
    /// Marks this prop as deferred with an optional group name.
    /// </summary>
    public void Defer(string? group = null)
    {
        _deferred = true;
        _group = group;
    }

    /// <summary>
    /// Whether this prop should be deferred.
    /// </summary>
    public bool ShouldDefer() => _deferred;

    /// <summary>
    /// The group name for batching deferred props, defaulting to "default".
    /// </summary>
    public string Group() => _group ?? "default";
}
