namespace InertiaCore.Core;

/// <summary>
/// Manages validation errors that persist through one redirect via TempData.
/// </summary>
public interface IInertiaErrorService
{
    /// <summary>
    /// Shares validation errors as an AlwaysProp on the given factory.
    /// Called by middleware before the request executes.
    /// </summary>
    void ShareErrors(IInertiaResponseFactory factory);

    /// <summary>
    /// Stores validation errors in TempData for the next request.
    /// </summary>
    void SetErrors(Dictionary<string, string> errors, string? errorBag = null);

    /// <summary>
    /// Keeps validation errors alive through a redirect.
    /// </summary>
    void Reflash();
}
