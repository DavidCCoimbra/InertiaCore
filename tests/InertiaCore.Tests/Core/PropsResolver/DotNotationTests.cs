namespace InertiaCore.Tests.Core.PropsResolver;

[Trait("Method", "ResolveAsync")]
public class DotNotationTests : PropsResolverTestBase
{
    [Fact]
    public async Task Expands_single_dot_key()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["user.name"] = "Alice",
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        var user = Assert.IsType<Dictionary<string, object?>>(props["user"]);
        Assert.Equal("Alice", user["name"]);
    }

    [Fact]
    public async Task Expands_multi_level_dot_key()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["user.address.city"] = "Lisbon",
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        var user = Assert.IsType<Dictionary<string, object?>>(props["user"]);
        var address = Assert.IsType<Dictionary<string, object?>>(user["address"]);
        Assert.Equal("Lisbon", address["city"]);
    }

    [Fact]
    public async Task Merges_dot_keys_into_existing_nested_dict()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?> { ["name"] = "Alice" },
            ["user.email"] = "alice@test.com",
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        var user = Assert.IsType<Dictionary<string, object?>>(props["user"]);
        Assert.Equal("Alice", user["name"]);
        Assert.Equal("alice@test.com", user["email"]);
    }

    [Fact]
    public async Task No_expansion_when_no_dot_keys()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["age"] = 30,
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        Assert.Equal("Alice", props["name"]);
        Assert.Equal(30, props["age"]);
    }

    [Fact]
    public async Task Dot_key_with_closure_resolves()
    {
        var resolver = CreateResolver();
        var page = new Dictionary<string, object?>
        {
            ["user.role"] = (Func<object?>)(() => "admin"),
        };

        var (props, _) = await resolver.ResolveAsync(new(), page);

        var user = Assert.IsType<Dictionary<string, object?>>(props["user"]);
        Assert.Equal("admin", user["role"]);
    }
}
