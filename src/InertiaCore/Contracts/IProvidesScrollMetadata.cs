namespace InertiaCore.Contracts;

/// <summary>
/// Provides pagination metadata for scroll-based props (infinite scroll, load more).
/// </summary>
public interface IProvidesScrollMetadata
{
    /// <summary>
    /// The query parameter name used for pagination (e.g., "page").
    /// </summary>
    string GetPageName();

    /// <summary>
    /// The previous page value, or null if on the first page.
    /// </summary>
    object? GetPreviousPage();

    /// <summary>
    /// The next page value, or null if on the last page.
    /// </summary>
    object? GetNextPage();

    /// <summary>
    /// The current page value.
    /// </summary>
    object? GetCurrentPage();
}
