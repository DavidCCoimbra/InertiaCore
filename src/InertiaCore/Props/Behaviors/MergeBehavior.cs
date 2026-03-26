namespace InertiaCore.Props.Behaviors;

/// <summary>
/// Configuration for merge behavior, including deep merge, prepend, and match strategies.
/// </summary>
public sealed class MergeBehavior
{
    private bool _merge;
    private bool _deepMerge;
    private bool _append = true;
    private readonly List<string> _matchOn = new();
    private readonly List<(string Path, string? MatchOn)> _appendsAtPaths = new();
    private readonly List<(string Path, string? MatchOn)> _prependsAtPaths = new();

    /// <summary>
    /// Enables shallow merge.
    /// </summary>
    public void EnableMerge() => _merge = true;

    /// <summary>
    /// Enables deep (recursive) merge.
    /// </summary>
    public void EnableDeepMerge()
    {
        _merge = true;
        _deepMerge = true;
    }

    /// <summary>
    /// Whether merging is enabled.
    /// </summary>
    public bool ShouldMerge() => _merge;

    /// <summary>
    /// Whether deep merge is enabled.
    /// </summary>
    public bool ShouldDeepMerge() => _deepMerge;

    /// <summary>
    /// Key matching strategies for deduplication, formatted as "path.key" strings.
    /// </summary>
    public string[] MatchesOn() => [.. _matchOn];

    /// <summary>
    /// Whether merge appends at the root level (default behavior).
    /// </summary>
    public bool ShouldAppendAtRoot() => _merge && _append
        && _appendsAtPaths.Count == 0 && _prependsAtPaths.Count == 0
        && !_deepMerge;

    /// <summary>
    /// Whether merge prepends at the root level.
    /// </summary>
    public bool ShouldPrependAtRoot() => _merge && !_append
        && _appendsAtPaths.Count == 0 && _prependsAtPaths.Count == 0
        && !_deepMerge;

    /// <summary>
    /// Sets key matching strategy for deduplication.
    /// </summary>
    public void SetMatchOn(params string[] keys) => _matchOn.AddRange(keys);

    /// <summary>
    /// Configures append at root or at a specific path.
    /// </summary>
    public void Append(string? path = null, string? matchOn = null)
    {
        _merge = true;
        _append = true;

        if (path != null)
        {
            _appendsAtPaths.Add((path, matchOn));
            if (matchOn != null)
            {
                _matchOn.Add($"{path}.{matchOn}");
            }

            return;
        }

        if (matchOn != null)
        {
            _matchOn.Add(matchOn);
        }
    }

    /// <summary>
    /// Configures prepend at root or at a specific path.
    /// </summary>
    public void Prepend(string? path = null, string? matchOn = null)
    {
        _merge = true;
        _append = false;

        if (path != null)
        {
            _prependsAtPaths.Add((path, matchOn));
            if (matchOn != null)
            {
                _matchOn.Add($"{path}.{matchOn}");
            }

            return;
        }

        if (matchOn != null)
        {
            _matchOn.Add(matchOn);
        }
    }

    /// <summary>
    /// Paths where appending is configured.
    /// </summary>
    public string[] GetAppendsAtPaths() =>
        [.. _appendsAtPaths.Select(x => x.Path)];

    /// <summary>
    /// Paths where prepending is configured.
    /// </summary>
    public string[] GetPrependsAtPaths() =>
        [.. _prependsAtPaths.Select(x => x.Path)];
}
