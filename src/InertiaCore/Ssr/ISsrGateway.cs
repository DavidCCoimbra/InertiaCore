namespace InertiaCore.Ssr;

/// <summary>
/// Abstracts communication with the SSR sidecar (Node.js server that renders components to HTML).
/// </summary>
public interface ISsrGateway
{
    /// <summary>
    /// Sends the page object to the SSR sidecar and returns the rendered HTML.
    /// Returns null if SSR is disabled, unavailable, or fails gracefully.
    /// </summary>
    Task<SsrResponse?> RenderAsync(Dictionary<string, object?> page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the SSR sidecar is reachable and responding.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
