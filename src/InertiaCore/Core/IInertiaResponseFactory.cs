namespace InertiaCore.Core;

/// <summary>
/// Scoped service that orchestrates Inertia responses. One instance per HTTP request.
/// </summary>
public interface IInertiaResponseFactory
{
    /// <summary>
    /// Creates an <see cref="InertiaResponse"/> for the given component and props dictionary.
    /// </summary>
    InertiaResponse Render(string component, Dictionary<string, object?>? props = null);

    /// <summary>
    /// Creates an <see cref="InertiaResponse"/> for the given component and anonymous object props.
    /// </summary>
    InertiaResponse Render(string component, object props);

    /// <summary>
    /// Creates an <see cref="InertiaResponse"/> for the given component with typed props.
    /// </summary>
    InertiaResponse Render<TProps>(string component, TProps props) where TProps : class;

    /// <summary>
    /// Adds a shared prop for this request.
    /// </summary>
    void Share(string key, object? value);

    /// <summary>
    /// Adds multiple shared props for this request.
    /// </summary>
    void Share(Dictionary<string, object?> props);

    /// <summary>
    /// Returns all shared props for this request.
    /// </summary>
    Dictionary<string, object?> GetShared();

    /// <summary>
    /// Returns a single shared prop by key, or null if not found.
    /// </summary>
    object? GetShared(string key);

    /// <summary>
    /// Overrides the root Razor view name for this request.
    /// </summary>
    void SetRootView(string view);

    /// <summary>
    /// Sets the asset version string for this request.
    /// </summary>
    void Version(string? version);

    /// <summary>
    /// Resolves the current asset version.
    /// </summary>
    string? GetVersion();

    /// <summary>
    /// Enables history encryption for this request.
    /// </summary>
    void EncryptHistory(bool encrypt = true);

    /// <summary>
    /// Signals the client to clear the history state after this response.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Signals the client to preserve the URL fragment across this redirect.
    /// </summary>
    void PreserveFragment();

    /// <summary>
    /// Stores flash data that will appear in the next response's page object.
    /// </summary>
    void Flash(string key, object? value);

    /// <summary>
    /// Stores multiple flash data entries.
    /// </summary>
    void Flash(Dictionary<string, object?> data);

    /// <summary>
    /// Returns the pending flash data for this request.
    /// </summary>
    Dictionary<string, object?> GetFlashed();

    /// <summary>
    /// Adds a once-resolved prop to shared props.
    /// </summary>
    void ShareOnce(string key, Func<object?> callback);

    /// <summary>
    /// Adds a once-resolved prop to shared props, wrapping an async callback.
    /// </summary>
    void ShareOnce(string key, Func<Task<object?>> callback);
}
