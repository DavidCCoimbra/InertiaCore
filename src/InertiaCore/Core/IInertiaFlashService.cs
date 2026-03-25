namespace InertiaCore.Core;

/// <summary>
/// Manages flash data that persists through one redirect via TempData.
/// </summary>
public interface IInertiaFlashService
{
    /// <summary>
    /// Stores a flash value for the next response.
    /// </summary>
    void Flash(string key, object? value);

    /// <summary>
    /// Stores multiple flash values for the next response.
    /// </summary>
    void Flash(Dictionary<string, object?> data);

    /// <summary>
    /// Returns the pending flash data for this request.
    /// </summary>
    Dictionary<string, object?> GetPending();

    /// <summary>
    /// Persists pending flash data to TempData so it survives the redirect.
    /// </summary>
    void Persist();

    /// <summary>
    /// Consumes flash data from TempData and returns it. Returns null if no flash data.
    /// </summary>
    Dictionary<string, object?>? Consume();

    /// <summary>
    /// Keeps flash data alive through a redirect by preventing TempData consumption.
    /// </summary>
    void Reflash();
}
