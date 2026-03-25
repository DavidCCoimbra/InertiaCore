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
    void ShareErrors(InertiaResponseFactory factory);

    /// <summary>
    /// Keeps validation errors alive through a redirect.
    /// </summary>
    void Reflash();
}
