namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Method", "ResolveAsync")]
public class MergeTests : PropsResolverTestBase
{
    [Fact]
    public async Task Merges_shared_and_page_props()
    {
        var resolver = CreateResolver();
        var shared = new Dictionary<string, object?> { ["appName"] = "MyApp" };
        var page = new Dictionary<string, object?> { ["user"] = "Alice" };

        var (props, _) = await resolver.ResolveAsync(shared, page);

        Assert.Equal("MyApp", props["appName"]);
        Assert.Equal("Alice", props["user"]);
    }

    [Fact]
    public async Task Page_props_override_shared_props()
    {
        var resolver = CreateResolver();
        var shared = new Dictionary<string, object?> { ["key"] = "shared" };
        var page = new Dictionary<string, object?> { ["key"] = "page" };

        var (props, _) = await resolver.ResolveAsync(shared, page);

        Assert.Equal("page", props["key"]);
    }

    [Fact]
    public async Task Preserves_null_values()
    {
        var resolver = CreateResolver();
        var shared = new Dictionary<string, object?>();
        var page = new Dictionary<string, object?> { ["nullable"] = null };

        var (props, _) = await resolver.ResolveAsync(shared, page);

        Assert.True(props.ContainsKey("nullable"));
        Assert.Null(props["nullable"]);
    }

    [Fact]
    public async Task Returns_empty_metadata_in_phase_1()
    {
        var resolver = CreateResolver();

        var (_, metadata) = await resolver.ResolveAsync(new(), new());

        Assert.Empty(metadata);
    }
}
