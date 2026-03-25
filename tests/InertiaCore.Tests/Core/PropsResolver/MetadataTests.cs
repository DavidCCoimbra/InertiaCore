using InertiaCore.Constants;
using InertiaCore.Props;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Method", "ResolveAsync")]
public class MetadataTests : PropsResolverTestBase
{
    [Fact]
    public async Task DeferProp_adds_deferredProps_metadata()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["charts"] = new DeferProp(() => (object?)"data", group: "analytics"),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.True(metadata.ContainsKey("deferredProps"));
        var deferred = (Dictionary<string, List<string>>)metadata["deferredProps"]!;
        Assert.True(deferred.ContainsKey("analytics"));
        Assert.Contains("charts", deferred["analytics"]);
    }

    [Fact]
    public async Task DeferProp_with_merge_includes_merge_flag_in_deferred_metadata()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["items"] = new DeferProp(() => (object?)"data").WithMerge(),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        var deferred = (Dictionary<string, List<string>>)metadata["deferredProps"]!;
        Assert.Contains("items", deferred["default"]);
        // Merge metadata for deferred+merge prop goes into mergeProps
        Assert.True(metadata.ContainsKey("mergeProps"));
        Assert.Contains("items", (List<string>)metadata["mergeProps"]!);
    }

    [Fact]
    public async Task MergeProp_adds_mergeProps_metadata()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["items"] = new MergeProp(new[] { 1, 2, 3 }),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.True(metadata.ContainsKey("mergeProps"));
        var merge = (List<string>)metadata["mergeProps"]!;
        Assert.Contains("items", merge);
    }

    [Fact]
    public async Task MergeProp_deep_merge_adds_deepMergeProps_metadata()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["config"] = new MergeProp(new { Theme = "dark" }).WithDeepMerge(),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.True(metadata.ContainsKey("deepMergeProps"));
        var deepMerge = (List<string>)metadata["deepMergeProps"]!;
        Assert.Contains("config", deepMerge);
        Assert.False(metadata.ContainsKey("mergeProps"));
    }

    [Fact]
    public async Task Reset_header_suppresses_merge_metadata()
    {
        var resolver = CreatePartialResolver("Home/Index", only: "items", reset: "items");
        var page = new Dictionary<string, object?>
        {
            ["items"] = new MergeProp(new[] { 1, 2, 3 }),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.False(metadata.ContainsKey("mergeProps"));
    }

    [Fact]
    public async Task No_metadata_when_no_special_props()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["age"] = 30,
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.Empty(metadata);
    }

    [Fact]
    public async Task Reset_header_suppresses_deferred_merge_flag()
    {
        var resolver = CreatePartialResolver("Home/Index", reset: "items");
        // On initial load (not partial for "items"), DeferProp is excluded and metadata collected
        // But since the resolver is partial here, it won't exclude — let's test via initial resolver
        var initialResolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["items"] = new DeferProp(() => (object?)"data").WithMerge(),
        };

        // Use a resolver with reset header but in initial mode
        var services = new ServiceCollection();
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.Reset] = "items";

        var resetResolver = new InertiaCore.Core.PropsResolver(
            services.BuildServiceProvider(), context.Request, component: null);

        var (_, metadata) = await resetResolver.ResolveAsync(new(), page);

        // Deferred metadata still present
        var deferred = (Dictionary<string, List<string>>)metadata["deferredProps"]!;
        Assert.Contains("items", deferred["default"]);
        // But merge metadata suppressed due to reset
        Assert.False(metadata.ContainsKey("mergeProps"));
    }

    [Fact]
    public async Task MergeProp_with_prepend_adds_prependProps_metadata()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["items"] = new MergeProp(new[] { 1, 2, 3 }).Prepend(),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.True(metadata.ContainsKey("prependProps"));
        var prepend = (List<string>)metadata["prependProps"]!;
        Assert.Contains("items", prepend);
        Assert.False(metadata.ContainsKey("mergeProps"));
    }

    [Fact]
    public async Task MergeProp_with_matchOn_adds_matchPropsOn_metadata()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["users"] = new MergeProp(new[] { 1 }).Append("data", "id"),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.True(metadata.ContainsKey("matchPropsOn"));
        var matchOn = (List<string>)metadata["matchPropsOn"]!;
        Assert.Contains("data.id", matchOn);
    }

    [Fact]
    public async Task OnceProp_on_initial_load_collects_onceProps_metadata()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["perms"] = new OnceProp(() => (object?)"admin").Until(TimeSpan.FromMinutes(30)),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.True(metadata.ContainsKey("onceProps"));
        var once = (Dictionary<string, object?>)metadata["onceProps"]!;
        Assert.True(once.ContainsKey("perms"));

        var entry = (Dictionary<string, object?>)once["perms"]!;
        Assert.Equal("perms", entry["prop"]);
        Assert.NotNull(entry["expiresAt"]);
    }

    [Fact]
    public async Task OptionalProp_with_once_collects_onceProps_metadata()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["lazy"] = new OptionalProp(() => (object?)"data").OnlyOnce("lazy-key"),
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.True(metadata.ContainsKey("onceProps"));
        var once = (Dictionary<string, object?>)metadata["onceProps"]!;
        Assert.True(once.ContainsKey("lazy"));
    }

    [Fact]
    public async Task SharedPropKeys_tracks_shared_prop_origins()
    {
        var resolver = CreateResolver();
        var shared = new Dictionary<string, object?>
        {
            ["appName"] = "MyApp",
            ["errors"] = new Dictionary<string, object?>(),
        };
        var page = new Dictionary<string, object?>
        {
            ["user"] = "Alice",
        };

        var (_, metadata) = await resolver.ResolveAsync(shared, page);

        Assert.True(metadata.ContainsKey("sharedProps"));
        var sharedKeys = (List<string>)metadata["sharedProps"]!;
        Assert.Contains("appName", sharedKeys);
        Assert.Contains("errors", sharedKeys);
        Assert.DoesNotContain("user", sharedKeys);
    }

    [Fact]
    public async Task No_sharedProps_metadata_when_no_shared()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        Assert.False(metadata.ContainsKey("sharedProps"));
    }

    [Fact]
    public async Task Multiple_deferred_props_grouped()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["chart1"] = new DeferProp(() => "a", group: "charts"),
            ["chart2"] = new DeferProp(() => "b", group: "charts"),
            ["sidebar"] = new DeferProp(() => "c")
        };

        var (_, metadata) = await resolver.ResolveAsync(new(), page);

        var deferred = (Dictionary<string, List<string>>)metadata["deferredProps"]!;
        Assert.Equal(2, deferred.Count); // "charts" and "default" groups

        Assert.Equal(2, deferred["charts"].Count);
        Assert.Contains("chart1", deferred["charts"]);
        Assert.Contains("chart2", deferred["charts"]);
        Assert.Single(deferred["default"]);
        Assert.Contains("sidebar", deferred["default"]);
    }
}
