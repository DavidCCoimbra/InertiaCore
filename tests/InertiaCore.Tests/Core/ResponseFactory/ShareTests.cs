namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Method", "Share")]
public class ShareTests : InertiaResponseFactoryTestBase
{
    [Fact]
    public void Accumulates_multiple_shared_props()
    {
        var factory = CreateFactory();

        factory.Share("key1", "value1");
        factory.Share("key2", "value2");

        var shared = factory.GetShared();
        Assert.Equal(2, shared.Count);
        Assert.Equal("value1", shared["key1"]);
        Assert.Equal("value2", shared["key2"]);
    }

    [Fact]
    public void Overwrites_existing_key()
    {
        var factory = CreateFactory();

        factory.Share("key", "original");
        factory.Share("key", "updated");

        Assert.Equal("updated", factory.GetShared("key"));
    }

    [Fact]
    public void Bulk_share_merges_props()
    {
        var factory = CreateFactory();
        factory.Share("existing", "stays");

        factory.Share(new Dictionary<string, object?>
        {
            ["new1"] = "a",
            ["new2"] = "b",
        });

        var shared = factory.GetShared();
        Assert.Equal(3, shared.Count);
        Assert.Equal("stays", shared["existing"]);
        Assert.Equal("a", shared["new1"]);
        Assert.Equal("b", shared["new2"]);
    }

    [Fact]
    public void Supports_null_values()
    {
        var factory = CreateFactory();

        factory.Share("nullable", null);

        Assert.Null(factory.GetShared("nullable"));
        Assert.Single(factory.GetShared());
    }
}
