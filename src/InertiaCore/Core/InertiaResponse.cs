namespace InertiaCore.Core;

/// <summary>
/// Inertia response that builds the page object and renders as JSON or Razor view.
/// </summary>
public class InertiaResponse
{
    internal string Component { get; }
    internal Dictionary<string, object?> Props { get; }
    internal Dictionary<string, object?> SharedProps { get; }
    internal string RootView { get; }
    internal string? Version { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaResponse"/>.
    /// </summary>
    public InertiaResponse(
        string component,
        Dictionary<string, object?> props,
        Dictionary<string, object?> sharedProps,
        string rootView,
        string? version)
    {
        Component = component;
        Props = props;
        SharedProps = sharedProps;
        RootView = rootView;
        Version = version;
    }
}
