using InertiaCore.Testing;
using InertiaCore.Tests.Helpers;

namespace InertiaCore.Tests.Testing;

[Trait("Class", "AssertableInertia")]
public class AssertableInertiaFailureTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public AssertableInertiaFailureTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<AssertableInertia> GetHomeAsync() =>
        await _client.GetInertiaAssertAsync("/", "1.0.0");

    private async Task<AssertableInertia> GetDashboardAsync() =>
        await _client.GetInertiaAssertAsync("/dashboard", "1.0.0");

    // -- HasUrl failure --

    [Fact]
    public async Task HasUrl_throws_on_mismatch()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasUrl("/wrong"));
    }

    // -- HasVersion failure --

    [Fact]
    public async Task HasVersion_throws_on_mismatch()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasVersion("wrong"));
    }

    // -- HasProp(key, value) failure --

    [Fact]
    public async Task HasProp_value_throws_on_wrong_value()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasProp("Greeting", "wrong"));
    }

    [Fact]
    public async Task HasProp_value_throws_when_key_missing()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasProp("missing", "value"));
    }

    // -- HasProp<T> failure --

    [Fact]
    public async Task HasProp_typed_throws_when_key_missing()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() =>
            inertia.HasProp<string>("missing", _ => { }));
    }

    // -- HasPropValue<T> failure --

    [Fact]
    public async Task HasPropValue_throws_when_missing()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasPropValue("missing", "val"));
    }

    // -- Where failure --

    [Fact]
    public async Task Where_throws_when_key_missing()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() =>
            inertia.Where("missing", _ => true));
    }

    // -- MissingMetadata failure --

    [Fact]
    public async Task MissingMetadata_throws_when_present()
    {
        var inertia = await GetDashboardAsync();
        Assert.Throws<AssertionException>(() => inertia.MissingMetadata("deferredProps"));
    }

    // -- HasDeferredProp failures --

    [Fact]
    public async Task HasDeferredProp_throws_wrong_group()
    {
        var inertia = await GetDashboardAsync();
        Assert.Throws<AssertionException>(() =>
            inertia.HasDeferredProp("stats", group: "wrong-group"));
    }

    [Fact]
    public async Task HasDeferredProp_throws_when_prop_not_in_deferred()
    {
        var inertia = await GetDashboardAsync();
        Assert.Throws<AssertionException>(() => inertia.HasDeferredProp("user"));
    }

    // -- HasDeepMergedProp failure --

    [Fact]
    public async Task HasDeepMergedProp_throws_when_not_deep_merged()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasDeepMergedProp("Greeting"));
    }

    // -- HasPrependedProp failure --

    [Fact]
    public async Task HasPrependedProp_throws_when_not_prepended()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasPrependedProp("Greeting"));
    }

    // -- HasOnceProp failure --

    [Fact]
    public async Task HasOnceProp_throws_when_no_once_metadata()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasOnceProp("something"));
    }

    // -- HasMatchOn failure --

    [Fact]
    public async Task HasMatchOn_throws_when_no_match_metadata()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasMatchOn("id"));
    }

    // -- History flag failures --

    [Fact]
    public async Task HasEncryptedHistory_throws_when_not_set()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasEncryptedHistory());
    }

    [Fact]
    public async Task HasClearHistory_throws_when_not_set()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasClearHistory());
    }

    [Fact]
    public async Task HasPreservedFragment_throws_when_not_set()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasPreservedFragment());
    }

    // -- Flash failures --

    [Fact]
    public async Task HasFlash_value_throws_when_wrong_value()
    {
        // No flash on initial load — need to trigger flash first
        // Just test the "no flash prop" case
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasFlash("success", "wrong"));
    }

    // -- Error failures --

    [Fact]
    public async Task HasError_with_message_throws_when_no_errors()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.HasError("name", "Required"));
    }

    [Fact]
    public async Task HasNoErrors_passes_with_empty_errors()
    {
        var inertia = await GetDashboardAsync();
        inertia.HasNoErrors();
    }

    // -- PropCount failure --

    [Fact]
    public async Task PropCount_throws_on_wrong_count()
    {
        var inertia = await GetHomeAsync();
        Assert.Throws<AssertionException>(() => inertia.PropCount(999));
    }

    // -- Response-level failures --

    [Fact]
    public async Task IsInertiaResponse_from_html_still_has_response()
    {
        var response = await _client.GetAsync("/");
        var inertia = await response.AssertInertiaAsync();

        // HTML response doesn't have X-Inertia header
        Assert.Throws<AssertionException>(() => inertia.IsInertiaResponse());
    }
}
