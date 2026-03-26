using System.Reflection;
using InertiaCore.Configuration;
using InertiaCore.Contracts;
using InertiaCore.Props;
using InertiaCore.Ssr;
using Microsoft.Extensions.Options;

namespace InertiaCore.Core;

/// <summary>
/// Scoped service that orchestrates Inertia responses. One instance per HTTP request.
/// </summary>
public class InertiaResponseFactory : IInertiaResponseFactory
{
    private readonly InertiaOptions _options;
    private readonly IInertiaFlashService _flashService;
    private readonly ISsrGateway? _ssrGateway;
    private string _rootView;
    private readonly Dictionary<string, object?> _sharedProps = new();
    private string? _version;
    private bool? _encryptHistory;
    private bool _clearHistory;
    private bool _preserveFragment;

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaResponseFactory"/>.
    /// </summary>
    public InertiaResponseFactory(
        IOptions<InertiaOptions> options,
        IInertiaFlashService flashService,
        ISsrGateway? ssrGateway = null)
    {
        _options = options.Value;
        _flashService = flashService;
        _ssrGateway = ssrGateway;
        _rootView = _options.RootView;
    }

    /// <summary>
    /// Creates an <see cref="InertiaResponse"/> for the given component and props dictionary.
    /// </summary>
    public InertiaResponse Render(string component, Dictionary<string, object?>? props = null)
    {
        var context = new InertiaResponseContext(
            RootView: _rootView,
            Version: GetVersion(),
            FlashService: _flashService,
            SsrGateway: _ssrGateway,
            SsrExcludedPaths: _options.Ssr.ExcludedPaths,
            EncryptHistory: _encryptHistory ?? _options.EncryptHistory,
            ClearHistory: _clearHistory,
            PreserveFragment: _preserveFragment);

        return new InertiaResponse(component, props ?? new(),
            new Dictionary<string, object?>(_sharedProps), context);
    }

    /// <summary>
    /// Creates an <see cref="InertiaResponse"/> for the given component and anonymous object props.
    /// </summary>
    public InertiaResponse Render(string component, object props) =>
        Render(component, ConvertToPropsDict(props));

    /// <summary>
    /// Adds a shared prop for this request.
    /// </summary>
    public void Share(string key, object? value) =>
        _sharedProps[key] = value;

    /// <summary>
    /// Adds multiple shared props for this request.
    /// </summary>
    public void Share(Dictionary<string, object?> props)
    {
        foreach (var (key, value) in props)
        {
            _sharedProps[key] = value;
        }
    }

    /// <summary>
    /// Returns all shared props for this request.
    /// </summary>
    public Dictionary<string, object?> GetShared() => _sharedProps;

    /// <summary>
    /// Returns a single shared prop by key, or null if not found.
    /// </summary>
    public object? GetShared(string key) => _sharedProps.GetValueOrDefault(key);

    /// <summary>
    /// Overrides the root Razor view name for this request.
    /// </summary>
    public void SetRootView(string view) => _rootView = view;

    /// <summary>
    /// Sets the asset version string for this request.
    /// </summary>
    public void Version(string? version) => _version = version;

    /// <summary>
    /// Resolves the current asset version. Per-request override takes precedence,
    /// then <see cref="InertiaOptions.VersionFunc"/>, then <see cref="InertiaOptions.Version"/>.
    /// </summary>
    public string? GetVersion() => _version ?? _options.ResolveVersion();

    // -- History flags --

    /// <summary>
    /// Enables history encryption for this request.
    /// </summary>
    public void EncryptHistory(bool encrypt = true) => _encryptHistory = encrypt;

    /// <summary>
    /// Signals the client to clear the history state after this response.
    /// </summary>
    public void ClearHistory() => _clearHistory = true;

    /// <summary>
    /// Signals the client to preserve the URL fragment across this redirect.
    /// </summary>
    public void PreserveFragment() => _preserveFragment = true;

    // -- Flash (delegates to InertiaFlashService) --

    /// <summary>
    /// Stores flash data that will appear in the next response's page object.
    /// </summary>
    public void Flash(string key, object? value) =>
        _flashService.Flash(key, value);

    /// <summary>
    /// Stores multiple flash data entries.
    /// </summary>
    public void Flash(Dictionary<string, object?> data) =>
        _flashService.Flash(data);

    /// <summary>
    /// Returns the pending flash data for this request.
    /// </summary>
    public Dictionary<string, object?> GetFlashed() =>
        _flashService.GetPending();

    // -- Prop factory methods --

    /// <summary>
    /// Creates a prop that is always included, even during partial reloads.
    /// </summary>
    public static AlwaysProp Always(object? value) => new(value);

    /// <summary>
    /// Creates a prop that is always included, wrapping a callback.
    /// </summary>
    public static AlwaysProp Always(Func<object?> callback) => new(callback);

    /// <summary>
    /// Creates a prop that is always included, wrapping an async callback.
    /// </summary>
    public static AlwaysProp Always(Func<Task<object?>> callback) => new(callback);

    /// <summary>
    /// Creates a prop excluded from the initial load, included when explicitly requested.
    /// </summary>
    public static OptionalProp Optional(Func<object?> callback) => new(callback);

    /// <summary>
    /// Creates a prop excluded from the initial load, wrapping an async callback.
    /// </summary>
    public static OptionalProp Optional(Func<Task<object?>> callback) => new(callback);

    /// <summary>
    /// Creates a deferred prop loaded asynchronously after initial render.
    /// </summary>
    public static DeferProp Defer(Func<object?> callback, string? group = null) => new(callback, group);

    /// <summary>
    /// Creates a deferred prop wrapping an async callback.
    /// </summary>
    public static DeferProp Defer(Func<Task<object?>> callback, string? group = null) => new(callback, group);

    /// <summary>
    /// Creates a prop that merges with existing client-side data.
    /// </summary>
    public static MergeProp Merge(object? value) => new(value);

    /// <summary>
    /// Creates a prop that merges with existing client-side data, wrapping a callback.
    /// </summary>
    public static MergeProp Merge(Func<object?> callback) => new(callback);

    /// <summary>
    /// Creates a prop that merges with existing client-side data, wrapping an async callback.
    /// </summary>
    public static MergeProp Merge(Func<Task<object?>> callback) => new(callback);

    /// <summary>
    /// Creates a prop resolved once and excluded on subsequent requests.
    /// </summary>
    public static OnceProp Once(Func<object?> callback) => new(callback);

    /// <summary>
    /// Creates a prop resolved once, wrapping an async callback.
    /// </summary>
    public static OnceProp Once(Func<Task<object?>> callback) => new(callback);

    /// <summary>
    /// Creates a scroll prop for infinite scroll patterns with pagination metadata.
    /// </summary>
    public static ScrollProp<T> Scroll<T>(T? value, string wrapper = "data", IProvidesScrollMetadata? metadataProvider = null) =>
        new(value, wrapper, metadataProvider);

    /// <summary>
    /// Creates a scroll prop wrapping a callback.
    /// </summary>
    public static ScrollProp<T> Scroll<T>(Func<T?> callback, string wrapper = "data", IProvidesScrollMetadata? metadataProvider = null) =>
        new(callback, wrapper, metadataProvider);

    /// <summary>
    /// Adds a once-resolved prop to shared props.
    /// </summary>
    public void ShareOnce(string key, Func<object?> callback) =>
        _sharedProps[key] = new OnceProp(callback);

    /// <summary>
    /// Adds a once-resolved prop to shared props, wrapping an async callback.
    /// </summary>
    public void ShareOnce(string key, Func<Task<object?>> callback) =>
        _sharedProps[key] = new OnceProp(callback);

    private static Dictionary<string, object?> ConvertToPropsDict(object props)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var property in props.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            dict[property.Name] = property.GetValue(props);
        }

        return dict;
    }
}
