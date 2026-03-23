namespace InertiaCore.Tests.Core.InertiaResponse;

[Trait("Method", "With")]
public class FluentBuilderTests : InertiaResponseTestBase
{
    [Fact]
    public async Task With_adds_prop_to_response()
    {
        var response = CreateResponse().With("extra", "value");
        var context = CreateInertiaHttpContext();

        await response.ExecuteAsync(context);

        var page = await ReadJsonResponse(context);
        Assert.Equal("value", page["props"].GetProperty("extra").GetString());
    }

    [Fact]
    public void With_returns_same_instance_for_chaining()
    {
        var response = CreateResponse();

        var result = response.With("key", "value");

        Assert.Same(response, result);
    }

    [Fact]
    public void WithViewData_returns_same_instance_for_chaining()
    {
        var response = CreateResponse();

        var result = response.WithViewData("key", "value");

        Assert.Same(response, result);
    }
}
