using InertiaCore.Ssr;
using Microsoft.Extensions.Logging;

namespace InertiaCore.Core;

/// <summary>
/// Configuration and services for an Inertia response, passed from the factory to the response.
/// </summary>
public record InertiaResponseContext(
    string RootView,
    string? Version,
    IInertiaFlashService? FlashService = null,
    ISsrGateway? SsrGateway = null,
    string[]? SsrExcludedPaths = null,
    bool EncryptHistory = false,
    bool ClearHistory = false,
    bool PreserveFragment = false,
    ILogger? Logger = null);
