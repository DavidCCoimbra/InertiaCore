namespace InertiaCore.Attributes;

/// <summary>
/// Marks a prop as resolved once and excluded on subsequent requests.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class InertiaOnceAttribute : Attribute
{
    /// <summary>
    /// Custom cache key for the once-resolved value.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// TTL in seconds for the cached value. 0 means no expiration.
    /// </summary>
    public int TtlSeconds { get; set; }
}
