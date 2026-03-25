namespace InertiaCore.Ssr;

/// <summary>
/// Represents the rendered HTML from the SSR sidecar.
/// </summary>
public record SsrResponse(string Head, string Body);
