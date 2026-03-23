namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Method", "ResolveAsync")]
public class NestedDictionaryTests : PropsResolverTestBase
{
    [Fact]
    public async Task Resolves_closures_in_nested_dictionaries()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["name"] = (Func<object?>)(() => "Alice"),
                ["age"] = 30,
            },
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        var user = Assert.IsType<Dictionary<string, object?>>(props["user"]);
        Assert.Equal("Alice", user["name"]);
        Assert.Equal(30, user["age"]);
    }

    [Fact]
    public async Task Resolves_deeply_nested_dictionaries()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["level1"] = new Dictionary<string, object?>
            {
                ["level2"] = new Dictionary<string, object?>
                {
                    ["value"] = (Func<object?>)(() => "deep"),
                },
            },
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        var level1 = Assert.IsType<Dictionary<string, object?>>(props["level1"]);
        var level2 = Assert.IsType<Dictionary<string, object?>>(level1["level2"]);
        Assert.Equal("deep", level2["value"]);
    }

    [Fact]
    public async Task Resolves_async_closures_in_nested_dictionaries()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["data"] = new Dictionary<string, object?>
            {
                ["items"] = (Func<Task<object?>>)(() => Task.FromResult<object?>(new[] { 1, 2, 3 })),
            },
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        var data = Assert.IsType<Dictionary<string, object?>>(props["data"]);
        Assert.Equal(new[] { 1, 2, 3 }, data["items"]);
    }
}
