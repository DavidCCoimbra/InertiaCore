namespace InertiaCore.Contracts;

/// <summary>
/// Base interface for all Inertia prop types. Provides async value resolution.
/// </summary>
public interface IInertiaProp
{
    /// <summary>
    /// Resolves the prop's value asynchronously.
    /// </summary>
    Task<object?> ResolveAsync(IServiceProvider services);
}
