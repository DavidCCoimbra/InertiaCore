using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using InertiaCore.Configuration;
using InertiaCore.Constants;
using Microsoft.Extensions.Options;

namespace InertiaCore.Core;

/// <summary>
/// In-memory cache for pre-serialized page data served via the async page data endpoint.
/// Each entry is scoped to the user identity that generated it.
/// </summary>
internal sealed class PageDataCache : IPageDataCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    private readonly int _ttlSeconds;
    private int _accessCount;

    public PageDataCache(IOptions<InertiaOptions> options)
    {
        _ttlSeconds = options.Value.Ssr.AsyncPageDataTtlSeconds;
    }

    public string Store(Dictionary<string, object?> page, string? userId)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(page, InertiaJsonOptions.CamelCase);
        var token = GenerateToken();

        _entries[token] = new CacheEntry(jsonBytes, DateTime.UtcNow, userId);

        // Lazy cleanup every 100 accesses
        if (Interlocked.Increment(ref _accessCount) % 100 == 0)
        {
            CleanupExpired();
        }

        return token;
    }

    public byte[]? TryGetBytes(string hash, string? userId)
    {
        // Remove on read — entry is single-use (one render → one fetch)
        if (!_entries.TryRemove(hash, out var entry))
            return null;

        if (DateTime.UtcNow - entry.CreatedAt > TimeSpan.FromSeconds(_ttlSeconds))
            return null;

        // Verify the requesting user matches the user who generated the entry
        if (entry.UserId != userId)
            return null;

        return entry.JsonBytes;
    }

    private static string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    private void CleanupExpired()
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromSeconds(_ttlSeconds);

        foreach (var (key, entry) in _entries)
        {
            if (entry.CreatedAt < cutoff)
            {
                _entries.TryRemove(key, out _);
            }
        }
    }

    private sealed record CacheEntry(byte[] JsonBytes, DateTime CreatedAt, string? UserId);
}
