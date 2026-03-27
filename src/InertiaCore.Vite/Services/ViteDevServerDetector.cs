using InertiaCore.Vite.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InertiaCore.Vite.Services;

/// <inheritdoc />
public sealed class ViteDevServerDetector(
    IWebHostEnvironment env,
    IOptions<ViteOptions> options) : IViteDevServerDetector
{
    private static readonly TimeSpan s_cacheDuration = TimeSpan.FromSeconds(2);

    private string? _cachedUrl;
    private DateTime _lastCheck;

    /// <inheritdoc />
    public bool IsRunning()
    {
        if (!env.IsDevelopment())
        {
            return false;
        }

        if (_cachedUrl is not null && DateTime.UtcNow - _lastCheck < s_cacheDuration)
        {
            return true;
        }

        var hotFilePath = Path.Combine(env.WebRootPath, options.Value.HotFilePath);

        if (!File.Exists(hotFilePath))
        {
            _cachedUrl = null;
            return false;
        }

        _cachedUrl = File.ReadAllText(hotFilePath).Trim();
        _lastCheck = DateTime.UtcNow;
        return true;
    }

    /// <inheritdoc />
    public string GetUrl() =>
        _cachedUrl ?? throw new InvalidOperationException("Dev server is not running.");
}
