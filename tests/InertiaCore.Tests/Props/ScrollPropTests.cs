using InertiaCore.Constants;
using InertiaCore.Contracts;
using InertiaCore.Props;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InertiaCore.Tests.Props;

[Trait("Class", "ScrollProp")]
public class ScrollPropTests
{
    private static readonly IServiceProvider s_emptyServices = new ServiceCollection().BuildServiceProvider();

    // -- Interface implementation --

    [Fact]
    public void Implements_IInertiaProp_IDeferrable_IMergeable()
    {
        var prop = new ScrollProp<int[]>([1, 2, 3]);

        Assert.IsAssignableFrom<IInertiaProp>(prop);
        Assert.IsAssignableFrom<IDeferrable>(prop);
        Assert.IsAssignableFrom<IMergeable>(prop);
    }

    [Fact]
    public void Merge_enabled_by_default()
    {
        var prop = new ScrollProp<int[]>([1, 2, 3]);

        Assert.True(prop.Merge.ShouldMerge());
    }

    [Fact]
    public void Not_deferred_by_default()
    {
        var prop = new ScrollProp<int[]>([1, 2, 3]);

        Assert.False(prop.Defer.ShouldDefer());
    }

    // -- Resolution --

    [Fact]
    public async Task Resolves_raw_value_wrapped_in_data_key()
    {
        var prop = new ScrollProp<int[]>([1, 2, 3]);

        var result = await prop.ResolveAsync(s_emptyServices);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(new[] { 1, 2, 3 }, dict["data"]);
    }

    [Fact]
    public async Task Resolves_with_custom_wrapper_key()
    {
        var prop = new ScrollProp<string[]>(["a", "b"], wrapper: "items");

        var result = await prop.ResolveAsync(s_emptyServices);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.True(dict.ContainsKey("items"));
        Assert.False(dict.ContainsKey("data"));
    }

    [Fact]
    public async Task Resolves_sync_callback()
    {
        var prop = new ScrollProp<string>(() => "computed");

        var result = await prop.ResolveAsync(s_emptyServices);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("computed", dict["data"]);
    }

    [Fact]
    public async Task Resolves_async_callback()
    {
        var prop = new ScrollProp<string>(() => Task.FromResult<string?>("async"));

        var result = await prop.ResolveAsync(s_emptyServices);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("async", dict["data"]);
    }

    [Fact]
    public async Task Resolves_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton("injected");
        var sp = services.BuildServiceProvider();

        var prop = new ScrollProp<string>(serviceProvider => serviceProvider.GetRequiredService<string>());

        var result = await prop.ResolveAsync(sp);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("injected", dict["data"]);
    }

    // -- Metadata --

    [Fact]
    public async Task Includes_metadata_from_provider()
    {
        var metadata = new TestScrollMetadata("page", 1, 2, 3);
        var prop = new ScrollProp<int[]>([1, 2, 3], metadataProvider: metadata);

        var result = await prop.ResolveAsync(s_emptyServices);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("page", dict["page"]);
        Assert.Equal(1, dict["prevPage"]);
        Assert.Equal(2, dict["nextPage"]);
        Assert.Equal(3, dict["currentPage"]);
    }

    [Fact]
    public async Task No_metadata_without_provider()
    {
        var prop = new ScrollProp<int[]>([1, 2, 3]);

        var result = await prop.ResolveAsync(s_emptyServices);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Single(dict); // Only "data" key
    }

    [Fact]
    public async Task Detects_metadata_from_value_implementing_interface()
    {
        var value = new ScrollableList([1, 2, 3], "page", null, 2, 1);
        var prop = new ScrollProp<ScrollableList>(value);

        var result = await prop.ResolveAsync(s_emptyServices);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("page", dict["page"]);
        Assert.Equal(2, dict["nextPage"]);
        Assert.Equal(1, dict["currentPage"]);
    }

    [Fact]
    public async Task Resolves_async_service_provider_callback()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new[] { "a", "b" });
        var sp = services.BuildServiceProvider();

        var prop = new ScrollProp<string[]>(
            async (IServiceProvider serviceProvider) =>
            {
                await Task.CompletedTask;
                return serviceProvider.GetRequiredService<string[]>();
            });

        var result = await prop.ResolveAsync(sp);

        var dict = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(new[] { "a", "b" }, dict["data"]);
    }

    // -- Defer --

    [Fact]
    public void WithDefer_enables_deferral()
    {
        var prop = new ScrollProp<int[]>([1, 2, 3]);

        var result = prop.WithDefer("scroll-group");

        Assert.Same(prop, result);
        Assert.True(prop.Defer.ShouldDefer());
        Assert.Equal("scroll-group", prop.Defer.Group());
    }

    // -- Merge intent --

    [Fact]
    public void ConfigureMergeIntent_prepend()
    {
        var prop = new ScrollProp<int[]>([1, 2, 3]);
        var context = new DefaultHttpContext();
        context.Request.Headers[InertiaHeaders.InfiniteScrollMergeIntent] = "prepend";

        prop.ConfigureMergeIntent(context);

        Assert.True(prop.Merge.PrependsAtRoot());
    }

    [Fact]
    public void ConfigureMergeIntent_default_appends()
    {
        var prop = new ScrollProp<int[]>([1, 2, 3]);
        var context = new DefaultHttpContext();

        prop.ConfigureMergeIntent(context);

        Assert.True(prop.Merge.AppendsAtRoot());
    }

    // -- Test helpers --

    private record TestScrollMetadata(
        string PageName, object? PreviousPage, object? NextPage, object? CurrentPage)
        : IProvidesScrollMetadata
    {
        public string GetPageName() => PageName;
        public object? GetPreviousPage() => PreviousPage;
        public object? GetNextPage() => NextPage;
        public object? GetCurrentPage() => CurrentPage;
    }

    private class ScrollableList(
        int[] items, string pageName, object? prevPage, object? nextPage, object? currentPage)
        : IProvidesScrollMetadata
    {
        public int[] Items { get; } = items;
        public string GetPageName() => pageName;
        public object? GetPreviousPage() => prevPage;
        public object? GetNextPage() => nextPage;
        public object? GetCurrentPage() => currentPage;
    }
}
