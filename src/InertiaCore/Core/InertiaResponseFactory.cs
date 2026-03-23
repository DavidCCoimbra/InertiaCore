using System.Reflection;
using InertiaCore.Configuration;
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
