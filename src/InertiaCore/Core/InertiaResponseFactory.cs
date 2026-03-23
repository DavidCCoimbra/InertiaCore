using System.Reflection;
using InertiaCore.Configuration;
using InertiaCore.Props;
using Microsoft.Extensions.Options;

namespace InertiaCore.Core;

/// <summary>
/// Scoped service that orchestrates Inertia responses. One instance per HTTP request.
/// </summary>
public class InertiaResponseFactory
{
    private readonly InertiaOptions _options;
    private string _rootView;
    private readonly Dictionary<string, object?> _sharedProps = new();
    private string? _version;

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaResponseFactory"/>.
    /// </summary>
    public InertiaResponseFactory(IOptions<InertiaOptions> options)
    {
        _options = options.Value;
        _rootView = _options.RootView;
    }

    /// <summary>
    /// Creates an <see cref="InertiaResponse"/> for the given component and props dictionary.
    /// </summary>
    public InertiaResponse Render(string component, Dictionary<string, object?>? props = null)
    {
        return new InertiaResponse(
            component: component,
            props: props ?? new(),
            sharedProps: new Dictionary<string, object?>(_sharedProps),
            rootView: _rootView,
            version: GetVersion()
        );
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
