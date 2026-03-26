namespace InertiaCore.Constants;

/// <summary>
/// HTTP header constants used by the Inertia.js protocol.
/// </summary>
public static class InertiaHeaders
{

    /// <summary>
    /// The main Inertia request header.
    /// </summary>
    public const string Inertia = "X-Inertia";

    /// <summary>
    /// Header for specifying which error bag to use for validation errors.
    /// </summary>
    public const string ErrorBag = "X-Inertia-Error-Bag";

    /// <summary>
    /// Header for external redirects.
    /// </summary>
    public const string Location = "X-Inertia-Location";

    /// <summary>
    /// Header for hash fragment redirects.
    /// </summary>
    public const string Redirect = "X-Inertia-Redirect";

    /// <summary>
    /// Header for the current asset version.
    /// </summary>
    public const string Version = "X-Inertia-Version";

    /// <summary>
    /// Header specifying the component for partial reloads.
    /// </summary>
    public const string PartialComponent = "X-Inertia-Partial-Component";

    /// <summary>
    /// Header specifying which props to include in partial reloads.
    /// </summary>
    public const string PartialData = "X-Inertia-Partial-Data";

    /// <summary>
    /// Header specifying which props to exclude from partial reloads.
    /// </summary>
    public const string PartialExcept = "X-Inertia-Partial-Except";

    /// <summary>
    /// Header for resetting the page state.
    /// </summary>
    public const string Reset = "X-Inertia-Reset";

    /// <summary>
    /// Header for specifying the merge intent when paginating on infinite scroll.
    /// </summary>
    public const string InfiniteScrollMergeIntent = "X-Inertia-Infinite-Scroll-Merge-Intent";

    /// <summary>
    /// Header specifying which once props to exclude from the response.
    /// </summary>
    public const string ExceptOnceProps = "X-Inertia-Except-Once-Props";
}
