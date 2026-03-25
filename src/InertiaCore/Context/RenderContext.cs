using Microsoft.AspNetCore.Http;

namespace InertiaCore.Context;

/// <summary>
/// Context passed to data providers containing the component name and HTTP request.
/// </summary>
public record RenderContext(string Component, HttpContext HttpContext);
