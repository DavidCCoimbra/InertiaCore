namespace InertiaCore.Props.Behaviors;

/// <summary>
/// Tracks once-resolution state for props that should only be resolved on the first request.
/// </summary>
public class OnceBehavior
{
    private bool _once;
    private bool _refresh;
    private string? _key;
    private DateTimeOffset? _expiresAtAbsolute;
    private TimeSpan? _ttl;

    /// <summary>
    /// Enables once-resolution for this prop.
    /// </summary>
    public void EnableOnce() => _once = true;

    /// <summary>
    /// Whether this prop should only be resolved once.
    /// </summary>
    public bool ShouldResolveOnce() => _once;

    /// <summary>
    /// Whether the client should re-resolve even if already cached.
    /// </summary>
    public bool ShouldBeRefreshed() => _refresh;

    /// <summary>
    /// The custom cache key, or null for auto-generated.
    /// </summary>
    public string? GetKey() => _key;

    /// <summary>
    /// Sets a custom identification key.
    /// </summary>
    public void SetKey(string key) => _key = key;

    /// <summary>
    /// Forces re-resolution even if the client has a cached value.
    /// </summary>
    public void SetRefresh(bool value = true) => _refresh = value;

    /// <summary>
    /// Sets a TTL duration from the time of resolution.
    /// </summary>
    public void SetTtl(TimeSpan ttl) => _ttl = ttl;

    /// <summary>
    /// Sets an absolute expiry time.
    /// </summary>
    public void SetExpiresAt(DateTimeOffset expiresAt) => _expiresAtAbsolute = expiresAt;

    /// <summary>
    /// Calculates expiry as Unix milliseconds, or null if no TTL is set.
    /// </summary>
    public long? ExpiresAt()
    {
        if (_expiresAtAbsolute != null)
        {
            return _expiresAtAbsolute.Value.ToUnixTimeMilliseconds();
        }

        if (_ttl != null)
        {
            return DateTimeOffset.UtcNow.Add(_ttl.Value).ToUnixTimeMilliseconds();
        }

        return null;
    }
}
