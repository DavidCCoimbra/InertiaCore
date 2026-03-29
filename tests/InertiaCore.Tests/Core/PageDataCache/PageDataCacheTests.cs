using System.Text.Json;
using InertiaCore.Configuration;
using InertiaCore.Core;
using Microsoft.Extensions.Options;

namespace InertiaCore.Tests.Core.PageDataCache;

[Trait("Class", "PageDataCache")]
public class PageDataCacheTests
{
    private static IPageDataCache CreateCache(int ttlSeconds = 30)
    {
        var options = Options.Create(new InertiaOptions
        {
            Ssr = new SsrOptions { AsyncPageDataTtlSeconds = ttlSeconds },
        });

        return new InertiaCore.Core.PageDataCache(options);
    }

    private static Dictionary<string, object?> CreatePage(string component = "Home", string url = "/")
    {
        return new Dictionary<string, object?>
        {
            ["component"] = component,
            ["props"] = new Dictionary<string, object?> { ["title"] = "Hello" },
            ["url"] = url,
            ["version"] = "abc123",
        };
    }

    [Fact]
    public void Store_returns_hex_token()
    {
        var cache = CreateCache();
        var page = CreatePage();

        var token = cache.Store(page, userId: null);

        Assert.Equal(32, token.Length);
        Assert.Matches("^[0-9a-f]{32}$", token);
    }

    [Fact]
    public void Store_returns_unique_tokens_for_same_page()
    {
        var cache = CreateCache();
        var page1 = CreatePage();
        var page2 = CreatePage();

        var token1 = cache.Store(page1, "user-1");
        var token2 = cache.Store(page2, "user-1");

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void Store_returns_different_tokens_for_different_pages()
    {
        var cache = CreateCache();
        var page1 = CreatePage("Home");
        var page2 = CreatePage("About");

        var hash1 = cache.Store(page1, userId: null);
        var hash2 = cache.Store(page2, userId: null);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Store_returns_different_hash_for_different_users_same_page()
    {
        var cache = CreateCache();
        var page = CreatePage();

        var hash1 = cache.Store(page, "admin");
        var hash2 = cache.Store(page, "regular-user");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void TryGetBytes_returns_data_for_matching_user()
    {
        var cache = CreateCache();
        var page = CreatePage();
        var hash = cache.Store(page, "user-1");

        var bytes = cache.TryGetBytes(hash, "user-1");

        Assert.NotNull(bytes);

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(bytes);
        Assert.NotNull(deserialized);
        Assert.Equal("Home", deserialized["component"].GetString());
    }

    [Fact]
    public void TryGetBytes_returns_null_for_wrong_user()
    {
        var cache = CreateCache();
        var page = CreatePage();
        var hash = cache.Store(page, "admin");

        var bytes = cache.TryGetBytes(hash, "attacker");

        Assert.Null(bytes);
    }

    [Fact]
    public void TryGetBytes_returns_null_for_null_user_when_stored_with_user()
    {
        var cache = CreateCache();
        var page = CreatePage();
        var hash = cache.Store(page, "admin");

        var bytes = cache.TryGetBytes(hash, userId: null);

        Assert.Null(bytes);
    }

    [Fact]
    public void TryGetBytes_returns_data_for_null_user_when_stored_without_user()
    {
        var cache = CreateCache();
        var page = CreatePage();
        var hash = cache.Store(page, userId: null);

        var bytes = cache.TryGetBytes(hash, userId: null);

        Assert.NotNull(bytes);
    }

    [Fact]
    public void TryGetBytes_returns_null_for_unknown_hash()
    {
        var cache = CreateCache();

        var bytes = cache.TryGetBytes("000000000000", userId: null);

        Assert.Null(bytes);
    }

    [Fact]
    public void TryGetBytes_returns_null_for_expired_entry()
    {
        var cache = CreateCache(ttlSeconds: 0);
        var page = CreatePage();
        var hash = cache.Store(page, userId: null);

        // TTL is 0 seconds, so it should be expired immediately
        Thread.Sleep(10);

        var bytes = cache.TryGetBytes(hash, userId: null);

        Assert.Null(bytes);
    }

    [Fact]
    public void Stored_json_uses_camelCase_naming()
    {
        var cache = CreateCache();
        var page = CreatePage();
        var hash = cache.Store(page, userId: null);

        var bytes = cache.TryGetBytes(hash, userId: null);
        var json = System.Text.Encoding.UTF8.GetString(bytes!);

        Assert.Contains("\"component\"", json);
        Assert.Contains("\"url\"", json);
        Assert.Contains("\"version\"", json);
    }
}
