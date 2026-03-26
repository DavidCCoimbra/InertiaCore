using System.Text.Json;

namespace InertiaCore.Constants;

/// <summary>
/// Shared JSON serialization options for the Inertia protocol.
/// </summary>
internal static class InertiaJsonOptions
{
    /// <summary>
    /// Default options with camelCase property naming for Inertia page objects.
    /// </summary>
    public static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
