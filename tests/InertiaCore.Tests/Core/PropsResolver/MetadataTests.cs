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
        var deferred = (List<Dictionary<string, object?>>)metadata["deferredProps"]!;
        Assert.Single(deferred);
        Assert.Equal("charts", deferred[0]["key"]);
        Assert.Equal("analytics", deferred[0]["group"]);
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

        var deferred = (List<Dictionary<string, object?>>)metadata["deferredProps"]!;
        Assert.Equal(true, deferred[0]["merge"]);
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

        // Deferred metadata should not include merge flag due to reset
        var deferred = (List<Dictionary<string, object?>>)metadata["deferredProps"]!;
        Assert.False(deferred[0].ContainsKey("merge"));
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

        var deferred = (List<Dictionary<string, object?>>)metadata["deferredProps"]!;
        Assert.Equal(3, deferred.Count);

        var chartsGroup = deferred.Where(d => (string)d["group"]! == "charts").ToList();
        Assert.Equal(2, chartsGroup.Count);
    }
}
