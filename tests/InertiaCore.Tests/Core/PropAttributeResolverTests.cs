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

        Assert.Equal("Alice", dict["Name"]);
        Assert.Equal(30, dict["Age"]);
    }

    // -- [InertiaAlways] --

    [Fact]
    public void Always_attribute_wraps_in_AlwaysProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new AlwaysProps("Alice"));

        Assert.IsType<AlwaysProp>(dict["Name"]);
    }

    [Fact]
    public async Task Always_attribute_resolves_to_original_value()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new AlwaysProps("Alice"));
        var prop = (IInertiaProp)dict["Name"]!;

        var result = await prop.ResolveAsync(Substitute.For<IServiceProvider>());

        Assert.Equal("Alice", result);
    }

    // -- [InertiaDefer] --

    [Fact]
    public void Defer_attribute_wraps_in_DeferProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferProps("heavy-data"));

        Assert.IsType<DeferProp>(dict["Stats"]);
    }

    [Fact]
    public void Defer_attribute_sets_group()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferProps("heavy-data"));
        var prop = (DeferProp)dict["Stats"]!;

        Assert.Equal("analytics", prop.Defer.Group());
    }

    [Fact]
    public void Defer_attribute_default_group()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferDefaultGroupProps("data"));
        var prop = (DeferProp)dict["Data"]!;

        Assert.Equal("default", prop.Defer.Group());
    }

    [Fact]
    public void Defer_attribute_implements_IIgnoreFirstLoad()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferProps("heavy-data"));

        Assert.IsAssignableFrom<IIgnoreFirstLoad>(dict["Stats"]);
    }

    // -- [InertiaMerge] --

    [Fact]
    public void Merge_attribute_wraps_in_MergeProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new MergeProps(new[] { 1, 2, 3 }));

        Assert.IsType<MergeProp>(dict["Items"]);
    }

    [Fact]
    public void Merge_attribute_enables_merge()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new MergeProps(new[] { 1, 2, 3 }));
        var prop = (MergeProp)dict["Items"]!;

        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Merge_deep_attribute_enables_deep_merge()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeepMergeProps(new { Theme = "dark" }));
        var prop = (MergeProp)dict["Config"]!;

        Assert.True(prop.Merge.ShouldDeepMerge());
    }

    [Fact]
    public void Merge_prepend_attribute_enables_prepend()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new PrependMergeProps(new[] { 1, 2 }));
        var prop = (MergeProp)dict["Items"]!;

        Assert.True(prop.Merge.ShouldPrependAtRoot());
    }

    // -- [InertiaOnce] --

    [Fact]
    public void Once_attribute_wraps_in_OnceProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceProps(new[] { "read", "write" }));

        Assert.IsType<OnceProp>(dict["Permissions"]);
    }

    [Fact]
    public void Once_attribute_enables_once_resolution()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceProps(new[] { "read", "write" }));
        var prop = (OnceProp)dict["Permissions"]!;

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    [Fact]
    public void Once_attribute_with_key_sets_key()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceWithKeyProps("admin"));
        var prop = (OnceProp)dict["Role"]!;

        Assert.Equal("user-role", prop.Once.GetKey());
    }

    [Fact]
    public void Once_attribute_with_ttl_produces_expiration_metadata()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OnceWithTtlProps("cached"));
        var prop = (OnceProp)dict["Data"]!;

        // TTL is set — ExpiresAt returns a non-null timestamp
        Assert.NotNull(prop.Once.ExpiresAt());
    }

    // -- [InertiaOptional] --

    [Fact]
    public void Optional_attribute_wraps_in_OptionalProp()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OptionalProps("lazy-data"));

        Assert.IsType<OptionalProp>(dict["Activity"]);
    }

    [Fact]
    public void Optional_attribute_implements_IIgnoreFirstLoad()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OptionalProps("lazy-data"));

        Assert.IsAssignableFrom<IIgnoreFirstLoad>(dict["Activity"]);
    }

    // -- Mixed attributes --

    [Fact]
    public void Mixed_props_record_wraps_each_property_correctly()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(
            new DashboardProps("Dashboard", "heavy", new[] { 1, 2 }, new[] { "read" }, "audit"));

        Assert.IsType<string>(dict["Title"]);
        Assert.IsType<DeferProp>(dict["Stats"]);
        Assert.IsType<MergeProp>(dict["Items"]);
        Assert.IsType<OnceProp>(dict["Permissions"]);
        Assert.IsType<OptionalProp>(dict["Activity"]);
    }

    [Fact]
    public async Task Mixed_props_all_resolve_to_original_values()
    {
        var props = new DashboardProps("Dashboard", "heavy", new[] { 1, 2 }, new[] { "read" }, "audit");
        var dict = PropAttributeResolver.ConvertToPropsDict(props);
        var services = Substitute.For<IServiceProvider>();

        Assert.Equal("Dashboard", dict["Title"]);
        Assert.Equal("heavy", await ((IInertiaProp)dict["Stats"]!).ResolveAsync(services));
        Assert.Equal(new[] { 1, 2 }, await ((IInertiaProp)dict["Items"]!).ResolveAsync(services));
        Assert.Equal(new[] { "read" }, await ((IInertiaProp)dict["Permissions"]!).ResolveAsync(services));
        Assert.Equal("audit", await ((IInertiaProp)dict["Activity"]!).ResolveAsync(services));
    }

    // -- Rule 1: IInertiaProp value skips attributes --

    [Fact]
    public void IInertiaProp_value_skips_attributes()
    {
        var deferProp = new DeferProp(() => (object?)"manual", "custom-group");
        var dict = PropAttributeResolver.ConvertToPropsDict(new PropTypeWithAttribute(deferProp));

        var prop = Assert.IsType<DeferProp>(dict["Stats"]);
        Assert.Equal("custom-group", prop.Defer.Group());
    }

    // -- Stacking: Defer + Merge --

    [Fact]
    public void Defer_plus_merge_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferMergeProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["Stats"]);

        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Defer_plus_deep_merge_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferDeepMergeProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["Stats"]);

        Assert.True(prop.Merge.ShouldDeepMerge());
    }

    [Fact]
    public void Defer_plus_prepend_merge_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferPrependProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["Items"]);

        Assert.True(prop.Merge.ShouldPrependAtRoot());
    }

    // -- Stacking: Defer + Once --

    [Fact]
    public void Defer_plus_once_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferOnceProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["Stats"]);

        Assert.True(prop.Once.ShouldResolveOnce());
    }

    // -- Stacking: Defer + Merge + Once (triple) --

    [Fact]
    public void Defer_plus_merge_plus_once_triple_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new DeferMergeOnceProps("data"));
        var prop = Assert.IsType<DeferProp>(dict["Stats"]);

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
        var prop = Assert.IsType<MergeProp>(dict["Items"]);

        Assert.True(prop.Merge.ShouldMerge());
        Assert.True(prop.Once.ShouldResolveOnce());
    }

    // -- Stacking: Optional + Once --

    [Fact]
    public void Optional_plus_once_stacks()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new OptionalOnceProps("lazy"));
        var prop = Assert.IsType<OptionalProp>(dict["Activity"]);

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
        var prop = Assert.IsType<MergeProp>(dict["Data"]);

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
        var prop = (IInertiaProp)dict["Stats"]!;

        var result = await prop.ResolveAsync(Substitute.For<IServiceProvider>());

        Assert.Null(result);
    }

    [Fact]
    public void Null_value_with_always_attribute_wraps()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new NullableAlwaysProps(null));

        Assert.IsType<AlwaysProp>(dict["User"]);
    }

    [Fact]
    public void Null_value_without_attribute_stays_null()
    {
        var dict = PropAttributeResolver.ConvertToPropsDict(new NullablePlainProps(null));

        Assert.Null(dict["Data"]);
    }

    // -- Caching --

    [Fact]
    public void Resolves_same_type_multiple_times_without_error()
    {
        var dict1 = PropAttributeResolver.ConvertToPropsDict(new PlainProps("Alice", 30));
        var dict2 = PropAttributeResolver.ConvertToPropsDict(new PlainProps("Bob", 25));

        Assert.Equal("Alice", dict1["Name"]);
        Assert.Equal("Bob", dict2["Name"]);
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
}
