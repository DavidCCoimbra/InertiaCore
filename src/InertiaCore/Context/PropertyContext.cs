using Microsoft.AspNetCore.Http;

namespace InertiaCore.Context;

/// <summary>
/// Context for individual property resolution during the props tree walk.
/// </summary>
public record PropertyContext(string Key, Dictionary<string, object?> Props, HttpContext HttpContext);
