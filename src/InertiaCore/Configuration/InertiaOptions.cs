using System.Text.Json.Serialization;

namespace InertiaCore.Configuration;

/// <summary>
/// Main configuration options for the Inertia server-side adapter.
/// </summary>
public sealed class InertiaOptions
{
    /// <summary>
    /// The Razor view that wraps the Inertia app.
    /// </summary>
    public string RootView { get; set; } = "App";

    /// <summary>
    /// Static asset version string. When set alongside <see cref="VersionFunc"/>,
    /// the func takes precedence.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Dynamic version resolver. Takes precedence over <see cref="Version"/> when both are set.
    /// Ignored during JSON/appsettings binding (not serializable).
    /// </summary>
    [JsonIgnore]
    public Func<string?>? VersionFunc { get; set; }

    /// <summary>
    /// Encrypt browser history state.
    /// </summary>
    public bool EncryptHistory { get; set; }

    /// <summary>
    /// Enables automatic validation error handling for MVC controllers.
    /// When enabled, Inertia requests with invalid ModelState auto-redirect with errors.
    /// </summary>
    public bool AutoValidation { get; set; }

    /// <summary>
    /// Server-side rendering configuration.
    /// </summary>
    public SsrOptions Ssr { get; set; } = new();

    /// <summary>
    /// Resolves the current asset version, preferring <see cref="VersionFunc"/>
    /// over the static <see cref="Version"/> property.
    /// </summary>
    public string? ResolveVersion() => VersionFunc?.Invoke() ?? Version;
}
