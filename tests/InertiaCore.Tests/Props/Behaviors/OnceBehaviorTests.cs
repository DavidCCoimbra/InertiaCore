using InertiaCore.Props.Behaviors;

namespace InertiaCore.Tests.Props.Behaviors;

[Trait("Class", "OnceBehavior")]
public class OnceBehaviorTests
{
    [Fact]
    public void Not_once_by_default()
    {
        var behavior = new OnceBehavior();

        Assert.False(behavior.ShouldResolveOnce());
        Assert.False(behavior.ShouldBeRefreshed());
        Assert.Null(behavior.GetKey());
        Assert.Null(behavior.ExpiresAt());
    }

    [Fact]
    public void EnableOnce_enables_resolution()
    {
        var behavior = new OnceBehavior();

        behavior.EnableOnce();

        Assert.True(behavior.ShouldResolveOnce());
    }

    [Fact]
    public void SetKey_sets_cache_key()
    {
        var behavior = new OnceBehavior();

        behavior.SetKey("user-permissions");

        Assert.Equal("user-permissions", behavior.GetKey());
    }

    [Fact]
    public void SetRefresh_enables_refresh()
    {
        var behavior = new OnceBehavior();

        behavior.SetRefresh();

        Assert.True(behavior.ShouldBeRefreshed());
    }

    [Fact]
    public void SetRefresh_false_disables_refresh()
    {
        var behavior = new OnceBehavior();

        behavior.SetRefresh();
        behavior.SetRefresh(false);

        Assert.False(behavior.ShouldBeRefreshed());
    }

    [Fact]
    public void ExpiresAt_returns_null_without_ttl()
    {
        var behavior = new OnceBehavior();

        Assert.Null(behavior.ExpiresAt());
    }

    [Fact]
    public void SetTtl_produces_future_expiry()
    {
        var behavior = new OnceBehavior();
        var before = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds();

        behavior.SetTtl(TimeSpan.FromMinutes(5));

        var expiry = behavior.ExpiresAt();
        var after = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeMilliseconds();

        Assert.NotNull(expiry);
        Assert.InRange(expiry.Value, before, after);
    }

    [Fact]
    public void SetExpiresAt_returns_exact_timestamp()
    {
        var behavior = new OnceBehavior();
        var target = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

        behavior.SetExpiresAt(target);

        Assert.Equal(target.ToUnixTimeMilliseconds(), behavior.ExpiresAt());
    }

    [Fact]
    public void Absolute_expiry_takes_precedence_over_ttl()
    {
        var behavior = new OnceBehavior();
        var absolute = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        behavior.SetTtl(TimeSpan.FromHours(1));
        behavior.SetExpiresAt(absolute);

        Assert.Equal(absolute.ToUnixTimeMilliseconds(), behavior.ExpiresAt());
    }
}
