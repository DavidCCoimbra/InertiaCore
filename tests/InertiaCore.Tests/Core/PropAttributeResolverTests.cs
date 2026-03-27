using InertiaCore.Attributes;
using InertiaCore.Contracts;
using InertiaCore.Core;
using InertiaCore.Props;
using InertiaCore.Props.Behaviors;
using NSubstitute;

namespace InertiaCore.Tests.Core;

[Trait("Class", "PropAttributeResolver")]
public class PropAttributeResolverTests
{
    // -- No attributes --

    [Fact]
    public void Property_without_attribute_is_raw_value()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new PlainProps("Alice", 30));

        Assert.Equal("Alice", dict["name"]);
        Assert.Equal(30, dict["age"]);
    }

    // -- [InertiaAlways] --

    [Fact]
    public void Always_attribute_wraps_in_AlwaysProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new AlwaysProps("Alice"));

        Assert.IsType<AlwaysProp>(dict["name"]);
    }

    [Fact]
    public async Task Always_attribute_resolves_to_original_value()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new AlwaysProps("Alice"));
        var prop = (IInertiaProp)dict["name"]!;

        var result = await prop.ResolveAsync(Substitute.For<IServiceProvider>());

        Assert.Equal("Alice", result);
    }

    // -- [InertiaDefer] --

    [Fact]
    public void Defer_attribute_wraps_in_DeferProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferProps("heavy-data"));

        Assert.IsType<DeferProp>(dict["stats"]);
    }

    [Fact]
    public void Defer_attribute_sets_group()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferProps("heavy-data"));
        var prop = (DeferProp)dict["stats"]!;

        Assert.Equal("analytics", prop.Defer.Group());
    }

    [Fact]
    public void Defer_attribute_default_group()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferDefaultGroupProps("data"));
        var prop = (DeferProp)dict["data"]!;

        Assert.Equal("default", prop.Defer.Group());
    }

    [Fact]
    public void Defer_attribute_implements_IIgnoreFirstLoad()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferProps("heavy-data"));

        Assert.IsAssignableFrom<IIgnoreFirstLoad>(dict["stats"]);
    }

    // -- [InertiaMerge] --

    [Fact]
    public void Merge_attribute_wraps_in_MergeProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new MergeProps(new[] { 1, 2, 3 }));

        Assert.IsType<MergeProp>(dict["items"]);
    }

    [Fact]
    public void Merge_attribute_enables_merge()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new MergeProps(new[] { 1, 2, 3 }));
        var prop = (MergeProp)dict["items"]!;

        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Merge_deep_attribute_enables_deep_merge()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeepMergeProps(new { Theme = "dark" }));
        var prop = (MergeProp)dict["config"]!;

        Assert.True(prop.Merge.ShouldDeepMerge());
    }

    [Fact]
    public void Merge_prepend_attribute_enables_prepend()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new PrependMergeProps(new[] { 1, 2 }));
        var prop = (MergeProp)dict["items"]!;

        Assert.True(prop.Merge.ShouldPrependAtRoot());
    }

    // -- [InertiaOnce] --

    [Fact]
    public void Once_attribute_wraps_in_OnceProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceProps(new[] { "read", "write" }));

        Assert.IsType<OnceProp>(dict["permissions"]);
    }

    [Fact]
    public void Once_attribute_enables_once_resolution()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceProps(new[] { "read", "write" }));
        var prop = (OnceProp)dict["permissions"]!;

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void Once_attribute_with_key_sets_key()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceWithKeyProps("admin"));
        var prop = (OnceProp)dict["role"]!;

        Assert.Equal("user-role", prop.Once.GetKey());
    }

    [Fact]
    public void Once_attribute_with_ttl_produces_expiration_metadata()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceWithTtlProps("cached"));
        var prop = (OnceProp)dict["data"]!;

        // TTL is set — ExpiresAt returns a non-null timestamp
        Assert.NotNull(prop.Once.ExpiresAt());
    }

    // -- [InertiaOptional] --

    [Fact]
    public void Optional_attribute_wraps_in_OptionalProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OptionalProps("lazy-data"));

        Assert.IsType<OptionalProp>(dict["activity"]);
    }

    [Fact]
    public void Optional_attribute_implements_IIgnoreFirstLoad()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OptionalProps("lazy-data"));

        Assert.IsAssignableFrom<IIgnoreFirstLoad>(dict["activity"]);
    }

    // -- Mixed attributes --

    [Fact]
    public void Mixed_props_record_wraps_each_property_correctly()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(
            new DashboardProps("Dashboard", "heavy", new[] { 1, 2 }, new[] { "read" }, "audit"));

        Assert.IsType<string>(dict["title"]);
        Assert.IsType<DeferProp>(dict["stats"]);
        Assert.IsType<MergeProp>(dict["items"]);
        Assert.IsType<OnceProp>(dict["permissions"]);
        Assert.IsType<OptionalProp>(dict["activity"]);
    }

    [Fact]
    public async Task Mixed_props_all_resolve_to_original_values()
    {
        var props = new DashboardProps("Dashboard", "heavy", new[] { 1, 2 }, new[] { "read" }, "audit");
        var dict = PropAttributeResolver.ConvertToPropsDict(props);
        var services = Substitute.For<IServiceProvider>();

        Assert.Equal("Dashboard", dict["title"]);
        Assert.Equal("heavy", await ((IInertiaProp)dict["stats"]!).ResolveAsync(services));
        Assert.Equal(new[] { 1, 2 }, await ((IInertiaProp)dict["items"]!).ResolveAsync(services));
        Assert.Equal(new[] { "read" }, await ((IInertiaProp)dict["permissions"]!).ResolveAsync(services));
        Assert.Equal("audit", await ((IInertiaProp)dict["activity"]!).ResolveAsync(services));
    }

    // -- Rule 1: IInertiaProp value skips attributes --

    [Fact]
    public void IInertiaProp_value_skips_attributes()
    {
        var deferProp = new DeferProp(() => (object?)"manual", "custom-group");
        var dict = PropAttributeResolver.ConvertToPropsDict(new PropTypeWithAttribute(deferProp));

        var prop = Assert.IsType<DeferProp>(dict["stats"]);
        Assert.Equal("custom-group", prop.Defer.Group());
    }

    // -- Stacking: Defer + Merge --

    [Fact]
    public void Defer_plus_merge_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferMergeProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["stats"]);

        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Defer_plus_deep_merge_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferDeepMergeProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["stats"]);

        Assert.True(prop.Merge.ShouldDeepMerge());
    }

    [Fact]
    public void Defer_plus_prepend_merge_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferPrependProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["items"]);

        Assert.True(prop.Merge.ShouldPrependAtRoot());
    }

    // -- Stacking: Defer + Once --

    [Fact]
    public void Defer_plus_once_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferOnceProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["stats"]);

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    // -- Stacking: Defer + Merge + Once (triple) --

    [Fact]
    public void Defer_plus_merge_plus_once_triple_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferMergeOnceProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["stats"]);

        Assert.Equal("analytics", prop.Defer.Group());
        Assert.True(prop.Merge.ShouldDeepMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
        Assert.NotNull(prop.Once.ExpiresAt());
    }

    // -- Stacking: Merge + Once --

    [Fact]
    public void Merge_plus_once_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new MergeOnceProps(new[] { 1, 2 }));
        var prop = Assert.IsType<MergeProp>(dict["items"]);

        Assert.True(prop.Merge.ShouldMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    // -- Stacking: Optional + Once --

    [Fact]
    public void Optional_plus_once_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OptionalOnceProps("lazy"));
        var prop = Assert.IsType<OptionalProp>(dict["activity"]);

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    // -- Validation: Invalid combos --

    [Fact]
    public void Always_plus_defer_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropAttributeResolver.ConvertToPropsDict(new AlwaysDeferProps("data")));

        Assert.Contains("[InertiaAlways] cannot be combined", ex.Message);
        Assert.Contains("Data", ex.Message);
    }

    [Fact]
    public void Always_plus_merge_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropAttributeResolver.ConvertToPropsDict(new AlwaysMergeProps("data")));

        Assert.Contains("[InertiaAlways] cannot be combined", ex.Message);
    }

    [Fact]
    public void Always_plus_once_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropAttributeResolver.ConvertToPropsDict(new AlwaysOnceProps("data")));

        Assert.Contains("[InertiaAlways] cannot be combined", ex.Message);
    }

    [Fact]
    public void Defer_plus_optional_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropAttributeResolver.ConvertToPropsDict(new DeferOptionalProps("data")));

        Assert.Contains("both base prop types", ex.Message);
        Assert.Contains("Data", ex.Message);
    }

    [Fact]
    public void Optional_plus_merge_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropAttributeResolver.ConvertToPropsDict(new OptionalMergeProps("data")));

        Assert.Contains("[InertiaMerge] cannot be used with [InertiaOptional]", ex.Message);
    }

    [Fact]
    public void Once_plus_merge_is_valid_merge_base()
    {
        // When both [InertiaOnce] and [InertiaMerge] are present, Merge is the base
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceMergeProps("data"));
        var prop = Assert.IsType<MergeProp>(dict["data"]);

        Assert.True(prop.Merge.ShouldMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    // -- Edge case: Deep + Prepend conflict --

    [Fact]
    public void Merge_deep_plus_prepend_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            PropAttributeResolver.ConvertToPropsDict(new DeepPrependConflictProps(new[] { 1 })));

        Assert.Contains("Deep and Prepend", ex.Message);
    }

    // -- Edge case: null values --

    [Fact]
    public async Task Null_value_with_defer_attribute_resolves_to_null()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new NullableDeferProps(null));
        var prop = (IInertiaProp)dict["stats"]!;

        var result = await prop.ResolveAsync(Substitute.For<IServiceProvider>());

        Assert.Null(result);
    }

    // -- [InertiaLive] --

    [Fact]
    public void Live_attribute_on_always_prop()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new LiveAlwaysProps("data"));
        var prop = (AlwaysProp)dict["notifications"]!;

        Assert.True(prop.Live.IsLive());
        Assert.Equal("notifications", prop.Live.Channel());
    }

    [Fact]
    public void Live_attribute_on_defer_prop()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new LiveDeferProps("data"));
        var prop = (DeferProp)dict["stats"]!;

        Assert.True(prop.Live.IsLive());
        Assert.Equal("dashboard-stats", prop.Live.Channel());
        Assert.Equal("analytics", prop.Defer.Group());
    }

    [Fact]
    public void Live_attribute_on_merge_prop()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new LiveMergeProps(["msg1"]));
        var prop = (MergeProp)dict["messages"]!;

        Assert.True(prop.Live.IsLive());
        Assert.Equal("chat", prop.Live.Channel());
        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Live_attribute_on_once_prop()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new LiveOnceProps(["read"]));
        var prop = (OnceProp)dict["permissions"]!;

        Assert.True(prop.Live.IsLive());
        Assert.Equal("permissions", prop.Live.Channel());
    }

    [Fact]
    public void Live_attribute_on_optional_prop()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new LiveOptionalProps("audit"));
        var prop = (OptionalProp)dict["activity"]!;

        Assert.True(prop.Live.IsLive());
        Assert.Equal("activity", prop.Live.Channel());
    }

    [Fact]
    public void Live_composes_with_defer_merge_once()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new LiveDeferMergeOnceProps("data"));
        var prop = (DeferProp)dict["stats"]!;

        Assert.True(prop.Live.IsLive());
        Assert.Equal("full-stack", prop.Live.Channel());
        Assert.Equal("analytics", prop.Defer.Group());
        Assert.True(prop.Merge.ShouldDeepMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void Live_fluent_api_works_on_all_prop_types()
    {
        var always = InertiaCore.Core.InertiaResponseFactory.Always("data").WithLive("ch1");
        var defer = InertiaCore.Core.InertiaResponseFactory.Defer(() => "data").WithLive("ch2");
        var merge = InertiaCore.Core.InertiaResponseFactory.Merge("data").WithLive("ch3");
        var once = InertiaCore.Core.InertiaResponseFactory.Once(() => "data").WithLive("ch4");
        var optional = InertiaCore.Core.InertiaResponseFactory.Optional(() => "data").WithLive("ch5");

        Assert.True(always.Live.IsLive());
        Assert.True(defer.Live.IsLive());
        Assert.True(merge.Live.IsLive());
        Assert.True(once.Live.IsLive());
        Assert.True(optional.Live.IsLive());
        Assert.Equal("ch1", always.Live.Channel());
        Assert.Equal("ch2", defer.Live.Channel());
        Assert.Equal("ch3", merge.Live.Channel());
        Assert.Equal("ch4", once.Live.Channel());
        Assert.Equal("ch5", optional.Live.Channel());
    }

    [Fact]
    public void Null_value_with_always_attribute_wraps()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new NullableAlwaysProps(null));

        Assert.IsType<AlwaysProp>(dict["user"]);
    }

    [Fact]
    public void Null_value_without_attribute_stays_null()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new NullablePlainProps(null));

        Assert.Null(dict["data"]);
    }

    // -- Caching --

    [Fact]
    public void Resolves_same_type_multiple_times_without_error()
    {
        var dict1 = PropAttributeResolver.ConvertToPropsDict(new PlainProps("Alice", 30));
        var dict2 = PropAttributeResolver.ConvertToPropsDict(new PlainProps("Bob", 25));

        Assert.Equal("Alice", dict1["name"]);
        Assert.Equal("Bob", dict2["name"]);
    }

    // -- [InertiaWhen] (conditional) --

    [Fact]
    public void When_attribute_includes_prop_when_condition_true()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(
            new ConditionalProps { IsAdmin = true, AdminPanel = "admin-data", PublicData = "public" });

        Assert.True(dict.ContainsKey("adminPanel"));
        Assert.Equal("admin-data", dict["adminPanel"]);
        Assert.Equal("public", dict["publicData"]);
    }

    [Fact]
    public void When_attribute_excludes_prop_when_condition_false()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(
            new ConditionalProps { IsAdmin = false, AdminPanel = "admin-data", PublicData = "public" });

        Assert.False(dict.ContainsKey("adminPanel"));
        Assert.Equal("public", dict["publicData"]);
    }

    [Fact]
    public void When_fluent_api_includes_when_true()
    {
        var dict = new Dictionary<string, object?>
        {
            ["admin"] = InertiaCore.Core.InertiaResponseFactory.When(true, () => "admin-data"),
            ["public"] = "public",
        };

        Assert.Equal("admin-data", dict["admin"]);
    }

    [Fact]
    public void When_fluent_api_returns_sentinel_when_false()
    {
        var result = InertiaCore.Core.InertiaResponseFactory.When(false, () => "admin-data");

        Assert.Same(InertiaCore.Core.ConditionalProp.Excluded, result);
    }

    [Fact]
    public void When_generic_fluent_api_works()
    {
        var included = InertiaCore.Core.InertiaResponseFactory.When(true, () => 42);
        var excluded = InertiaCore.Core.InertiaResponseFactory.When(false, () => 42);

        Assert.Equal(42, included);
        Assert.Same(InertiaCore.Core.ConditionalProp.Excluded, excluded);
    }

    // -- Fallback --

    [Fact]
    public void Fallback_fluent_api_works()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => "data")
            .WithFallback("default-value");

        Assert.True(prop.Fallback.HasFallback());
        Assert.Equal("default-value", prop.Fallback.GetFallback());
    }

    [Fact]
    public void Fallback_attribute_sets_fallback()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new FallbackDeferProps("data"));
        var prop = (DeferProp)dict["stats"]!;

        Assert.True(prop.Fallback.HasFallback());
        Assert.IsType<FallbackStats>(prop.Fallback.GetFallback());
    }

    [Fact]
    public void Fallback_composable_with_all_types()
    {
        var always = InertiaCore.Core.InertiaResponseFactory.Always("x").WithFallback("fb");
        var merge = InertiaCore.Core.InertiaResponseFactory.Merge("x").WithFallback("fb");
        var once = InertiaCore.Core.InertiaResponseFactory.Once(() => "x").WithFallback("fb");
        var optional = InertiaCore.Core.InertiaResponseFactory.Optional(() => "x").WithFallback("fb");

        Assert.True(always.Fallback.HasFallback());
        Assert.True(merge.Fallback.HasFallback());
        Assert.True(once.Fallback.HasFallback());
        Assert.True(optional.Fallback.HasFallback());
    }

    // -- Timed --

    [Fact]
    public void Timed_fluent_api_works()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Always("time")
            .RefreshEvery(TimeSpan.FromSeconds(30));

        Assert.True(prop.Timed.IsTimed());
        Assert.Equal(30000, prop.Timed.IntervalMs());
    }

    [Fact]
    public void Timed_attribute_on_always_prop()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new TimedAlwaysProps("now"));
        var prop = (AlwaysProp)dict["serverTime"]!;

        Assert.True(prop.Timed.IsTimed());
        Assert.Equal(30000, prop.Timed.IntervalMs());
    }

    [Fact]
    public void Timed_attribute_on_defer_prop()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new TimedDeferProps("99.50"));
        var prop = (DeferProp)dict["price"]!;

        Assert.True(prop.Timed.IsTimed());
        Assert.Equal(5000, prop.Timed.IntervalMs());
    }

    [Fact]
    public void Timed_composable_with_live()
    {
        var prop = InertiaCore.Core.InertiaResponseFactory.Defer(() => "data")
            .WithLive("channel")
            .RefreshEvery(TimeSpan.FromSeconds(10));

        Assert.True(prop.Live.IsLive());
        Assert.True(prop.Timed.IsTimed());
    }

    // -- Test records: single attributes --

    private record PlainProps(string Name, int Age);
    private record AlwaysProps([property: InertiaAlways] string Name);
    private record DeferProps([property: InertiaDefer(Group = "analytics")] string Stats);
    private record DeferDefaultGroupProps([property: InertiaDefer] string Data);
    private record MergeProps([property: InertiaMerge] int[] Items);
    private record DeepMergeProps([property: InertiaMerge(Deep = true)] object Config);
    private record PrependMergeProps([property: InertiaMerge(Prepend = true)] int[] Items);
    private record OnceProps([property: InertiaOnce] string[] Permissions);
    private record OnceWithKeyProps([property: InertiaOnce(Key = "user-role")] string Role);
    private record OnceWithTtlProps([property: InertiaOnce(TtlSeconds = 3600)] string Data);
    private record OptionalProps([property: InertiaOptional] string Activity);

    private record DashboardProps(
        string Title,
        [property: InertiaDefer(Group = "analytics")] string Stats,
        [property: InertiaMerge] int[] Items,
        [property: InertiaOnce] string[] Permissions,
        [property: InertiaOptional] string Activity);

    // -- Test records: Rule 1 (prop type + attribute) --

    private record PropTypeWithAttribute([property: InertiaMerge] DeferProp Stats);

    // -- Test records: valid stacking --

    private record DeferMergeProps(
        [property: InertiaDefer][property: InertiaMerge] string Stats);

    private record DeferDeepMergeProps(
        [property: InertiaDefer][property: InertiaMerge(Deep = true)] string Stats);

    private record DeferPrependProps(
        [property: InertiaDefer][property: InertiaMerge(Prepend = true)] string Items);

    private record DeferOnceProps(
        [property: InertiaDefer][property: InertiaOnce] string Stats);

    private record DeferMergeOnceProps(
        [property: InertiaDefer(Group = "analytics")]
        [property: InertiaMerge(Deep = true)]
        [property: InertiaOnce(TtlSeconds = 3600)] string Stats);

    private record MergeOnceProps(
        [property: InertiaMerge][property: InertiaOnce] int[] Items);

    private record OptionalOnceProps(
        [property: InertiaOptional][property: InertiaOnce] string Activity);

    // -- Test records: invalid combos --

    private record AlwaysDeferProps(
        [property: InertiaAlways][property: InertiaDefer] string Data);

    private record AlwaysMergeProps(
        [property: InertiaAlways][property: InertiaMerge] string Data);

    private record AlwaysOnceProps(
        [property: InertiaAlways][property: InertiaOnce] string Data);

    private record DeferOptionalProps(
        [property: InertiaDefer][property: InertiaOptional] string Data);

    private record OptionalMergeProps(
        [property: InertiaOptional][property: InertiaMerge] string Data);

    private record OnceMergeProps(
        [property: InertiaOnce][property: InertiaMerge] string Data);

    // -- Test records: edge cases --

    private record DeepPrependConflictProps(
        [property: InertiaMerge(Deep = true, Prepend = true)] int[] Items);

    private record NullableDeferProps(
        [property: InertiaDefer] string? Stats);

    private record NullableAlwaysProps(
        [property: InertiaAlways] string? User);

    private record NullablePlainProps(string? Data);

    // -- Test records: live --

    private record LiveAlwaysProps(
        [property: InertiaAlways][property: InertiaLive(Channel = "notifications")] string Notifications);

    private record LiveDeferProps(
        [property: InertiaDefer(Group = "analytics")][property: InertiaLive(Channel = "dashboard-stats")] string Stats);

    private record LiveMergeProps(
        [property: InertiaMerge][property: InertiaLive(Channel = "chat")] string[] Messages);

    private record LiveOnceProps(
        [property: InertiaOnce][property: InertiaLive(Channel = "permissions")] string[] Permissions);

    private record LiveOptionalProps(
        [property: InertiaOptional][property: InertiaLive(Channel = "activity")] string Activity);

    private record LiveDeferMergeOnceProps(
        [property: InertiaDefer(Group = "analytics")]
        [property: InertiaMerge(Deep = true)]
        [property: InertiaOnce(TtlSeconds = 3600)]
        [property: InertiaLive(Channel = "full-stack")] string Stats);

    // -- Test records: conditional --

    private class ConditionalProps
    {
        public bool IsAdmin { get; init; }

        [InertiaWhen(nameof(IsAdmin))]
        public string? AdminPanel { get; init; }

        public string? PublicData { get; init; }
    }

    // -- Test records: fallback --

    private class FallbackStats
    {
        public int Total { get; init; }
    }

    private record FallbackDeferProps(
        [property: InertiaDefer][property: InertiaFallback(typeof(FallbackStats))] string Stats);

    // -- Test records: timed --

    private record TimedAlwaysProps(
        [property: InertiaAlways][property: InertiaTimed(IntervalSeconds = 30)] string ServerTime);

    private record TimedDeferProps(
        [property: InertiaDefer][property: InertiaTimed(IntervalSeconds = 5)] string Price);
}
