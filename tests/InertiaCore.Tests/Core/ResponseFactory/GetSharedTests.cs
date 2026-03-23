namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Method", "GetShared")]
public class GetSharedTests : InertiaResponseFactoryTestBase
{
    [Fact]
    public void Returns_null_for_missing_key()
    {
        var factory = CreateFactory();

        Assert.Null(factory.GetShared("nonexistent"));
    }

    [Fact]
    public void Returns_value_for_existing_key()
    {
        var factory = CreateFactory();
        factory.Share("key", 42);

        Assert.Equal(42, factory.GetShared("key"));
    }
}
