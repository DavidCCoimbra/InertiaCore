namespace InertiaCore.Core;

/// <summary>
/// In-memory cache for page data served via the async page data endpoint.
/// Entries are scoped to the user identity that generated them.
/// </summary>
public interface IPageDataCache
{
    /// <summary>
    /// Stores a page object and returns a hash key for retrieval.
    /// The entry is scoped to the given user identity.
    /// </summary>
    string Store(Dictionary<string, object?> page, string? userId);

    /// <summary>
    /// Retrieves the pre-serialized JSON bytes for a cached page.
    /// Returns null if expired, missing, or the user identity doesn't match.
    /// </summary>
    byte[]? TryGetBytes(string hash, string? userId);
}
