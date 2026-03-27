namespace InertiaCore.Vite.Constants;

/// <summary>
/// Inline JavaScript snippets emitted by Vite Tag Helpers.
/// </summary>
internal static class ViteScripts
{
    /// <summary>
    /// React refresh preamble script. The {0} placeholder is replaced with the encoded dev server URL at runtime.
    /// </summary>
    public const string ReactRefreshPreamble =
        """
        import RefreshRuntime from '{0}'
        RefreshRuntime.injectIntoGlobalHook(window)
        window.$RefreshReg$ = () => {{}}
        window.$RefreshSig$ = () => (type) => type
        window.__vite_plugin_react_preamble_installed__ = true
        """;
}
