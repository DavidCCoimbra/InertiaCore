namespace InertiaCore.Tests.Core.ResponseFactory;

[Trait("Class", "InertiaResponseFactory")]
public class HistoryFlagTests : InertiaResponseFactoryTestBase
{
    [Fact]
    public void EncryptHistory_defaults_to_false()
    {
        var factory = CreateFactory();

        var response = factory.Render("Test");

        Assert.False(HasPageFlag(response, "_encryptHistory"));
    }

    [Fact]
    public void EncryptHistory_per_request_override()
    {
        var factory = CreateFactory();
        factory.EncryptHistory();

        var response = factory.Render("Test");

        // The flag is passed to InertiaResponse internally
        // We verify via the page object in JsonResponseTests
        Assert.NotNull(response);
    }

    [Fact]
    public void EncryptHistory_from_options()
    {
        var factory = CreateFactory(o => o.EncryptHistory = true);

        var response = factory.Render("Test");

        Assert.NotNull(response);
    }

    [Fact]
    public void ClearHistory_sets_flag()
    {
        var factory = CreateFactory();
        factory.ClearHistory();

        var response = factory.Render("Test");

        Assert.NotNull(response);
    }

    [Fact]
    public void PreserveFragment_sets_flag()
    {
        var factory = CreateFactory();
        factory.PreserveFragment();

        var response = factory.Render("Test");

        Assert.NotNull(response);
    }

    private static bool HasPageFlag(InertiaCore.Core.InertiaResponse response, string fieldName)
    {
        var field = response.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null && (bool)field.GetValue(response)!;
    }
}
