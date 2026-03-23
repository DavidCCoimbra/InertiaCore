using InertiaCore.Props.Behaviors;

namespace InertiaCore.Tests.Props.Behaviors;

[Trait("Class", "MergeBehavior")]
public class MergeBehaviorTests
{
    [Fact]
    public void Not_merged_by_default()
    {
        var behavior = new MergeBehavior();

        Assert.False(behavior.ShouldMerge());
        Assert.False(behavior.ShouldDeepMerge());
    }

    [Fact]
    public void EnableMerge_enables_shallow_merge()
    {
        var behavior = new MergeBehavior();

        behavior.EnableMerge();

        Assert.True(behavior.ShouldMerge());
        Assert.False(behavior.ShouldDeepMerge());
    }

    [Fact]
    public void EnableDeepMerge_enables_both_flags()
    {
        var behavior = new MergeBehavior();

        behavior.EnableDeepMerge();

        Assert.True(behavior.ShouldMerge());
        Assert.True(behavior.ShouldDeepMerge());
    }

    [Fact]
    public void AppendsAtRoot_true_for_default_merge()
    {
        var behavior = new MergeBehavior();

        behavior.EnableMerge();

        Assert.True(behavior.AppendsAtRoot());
        Assert.False(behavior.PrependsAtRoot());
    }

    [Fact]
    public void AppendsAtRoot_false_when_not_merged()
    {
        var behavior = new MergeBehavior();

        Assert.False(behavior.AppendsAtRoot());
    }

    [Fact]
    public void AppendsAtRoot_false_when_deep_merge()
    {
        var behavior = new MergeBehavior();

        behavior.EnableDeepMerge();

        Assert.False(behavior.AppendsAtRoot());
        Assert.False(behavior.PrependsAtRoot());
    }

    [Fact]
    public void Prepend_at_root_sets_prepend_flag()
    {
        var behavior = new MergeBehavior();

        behavior.Prepend();

        Assert.True(behavior.ShouldMerge());
        Assert.True(behavior.PrependsAtRoot());
        Assert.False(behavior.AppendsAtRoot());
    }

    [Fact]
    public void Append_with_path_tracks_path()
    {
        var behavior = new MergeBehavior();

        behavior.Append("data.items");

        Assert.True(behavior.ShouldMerge());
        Assert.Equal(["data.items"], behavior.GetAppendsAtPaths());
        Assert.False(behavior.AppendsAtRoot());
    }

    [Fact]
    public void Prepend_with_path_tracks_path()
    {
        var behavior = new MergeBehavior();

        behavior.Prepend("data.items");

        Assert.True(behavior.ShouldMerge());
        Assert.Equal(["data.items"], behavior.GetPrependsAtPaths());
        Assert.False(behavior.PrependsAtRoot());
    }

    [Fact]
    public void Prepend_with_path_and_matchOn_adds_match_key()
    {
        var behavior = new MergeBehavior();

        behavior.Prepend("users", "id");

        Assert.Equal(["users.id"], behavior.MatchesOn());
    }

    [Fact]
    public void Prepend_without_path_but_with_matchOn_adds_root_match()
    {
        var behavior = new MergeBehavior();

        behavior.Prepend(matchOn: "id");

        Assert.Equal(["id"], behavior.MatchesOn());
    }

    [Fact]
    public void Append_with_path_and_matchOn_adds_match_key()
    {
        var behavior = new MergeBehavior();

        behavior.Append("users", "id");

        Assert.Equal(["users.id"], behavior.MatchesOn());
    }

    [Fact]
    public void Append_without_path_but_with_matchOn_adds_root_match()
    {
        var behavior = new MergeBehavior();

        behavior.Append(matchOn: "id");

        Assert.Equal(["id"], behavior.MatchesOn());
    }

    [Fact]
    public void SetMatchOn_adds_keys()
    {
        var behavior = new MergeBehavior();

        behavior.SetMatchOn("id", "slug");

        Assert.Equal(["id", "slug"], behavior.MatchesOn());
    }

    [Fact]
    public void Multiple_appends_accumulate_paths()
    {
        var behavior = new MergeBehavior();

        behavior.Append("data.users");
        behavior.Append("data.posts");

        Assert.Equal(["data.users", "data.posts"], behavior.GetAppendsAtPaths());
    }

    [Fact]
    public void Empty_paths_by_default()
    {
        var behavior = new MergeBehavior();

        Assert.Empty(behavior.GetAppendsAtPaths());
        Assert.Empty(behavior.GetPrependsAtPaths());
        Assert.Empty(behavior.MatchesOn());
    }
}
