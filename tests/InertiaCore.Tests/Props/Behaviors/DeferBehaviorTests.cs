using InertiaCore.Props.Behaviors;

namespace InertiaCore.Tests.Props.Behaviors;

[Trait("Class", "DeferBehavior")]
public class DeferBehaviorTests
{
    [Fact]
    public void Not_deferred_by_default()
    {
        var behavior = new DeferBehavior();

        Assert.False(behavior.ShouldDefer());
    }

    [Fact]
    public void Defaults_to_default_group()
    {
        var behavior = new DeferBehavior();

        Assert.Equal("default", behavior.Group());
    }

    [Fact]
    public void Defer_enables_deferral()
    {
        var behavior = new DeferBehavior();

        behavior.Defer();

        Assert.True(behavior.ShouldDefer());
    }

    [Fact]
    public void Defer_with_group_sets_group_name()
    {
        var behavior = new DeferBehavior();

        behavior.Defer("charts");

        Assert.True(behavior.ShouldDefer());
        Assert.Equal("charts", behavior.Group());
    }

    [Fact]
    public void Defer_without_group_uses_default()
    {
        var behavior = new DeferBehavior();

        behavior.Defer();

        Assert.Equal("default", behavior.Group());
    }
}
