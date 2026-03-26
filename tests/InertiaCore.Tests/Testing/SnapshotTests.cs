using System.Text.Json;
using InertiaCore.Testing;

namespace InertiaCore.Tests.Testing;

[Trait("Class", "AssertableInertia")]
[Trait("Method", "MatchesSnapshot")]
public class SnapshotTests : IDisposable
{
    private readonly string _snapshotDir;

    public SnapshotTests()
    {
        _snapshotDir = Path.Combine(
            Path.GetDirectoryName(GetCallerFilePath())!,
            "__snapshots__");
    }

    [Fact]
    public void Creates_snapshot_on_first_run()
    {
        CleanSnapshot(nameof(Creates_snapshot_on_first_run));
        var inertia = CreateAssertable(new { component = "Test", url = "/", props = new { name = "Alice" } });

        inertia.MatchesSnapshot();

        var snapshotPath = GetSnapshotPath(nameof(Creates_snapshot_on_first_run));
        Assert.True(File.Exists(snapshotPath));
    }

    [Fact]
    public void Matches_existing_snapshot()
    {
        var methodName = nameof(Matches_existing_snapshot);
        var page = new { component = "Test", url = "/", props = new { name = "Bob" } };
        var inertia = CreateAssertable(page);

        // Create snapshot
        CleanSnapshot(methodName);
        inertia.MatchesSnapshot();

        // Should match on second call
        var inertia2 = CreateAssertable(page);
        inertia2.MatchesSnapshot();
    }

    [Fact]
    public void Throws_on_mismatch()
    {
        var methodName = nameof(Throws_on_mismatch);
        CleanSnapshot(methodName);

        // Create snapshot with original data
        var original = CreateAssertable(new { component = "Test", url = "/", props = new { name = "Alice" } });
        original.MatchesSnapshot();

        // Different data should fail
        var changed = CreateAssertable(new { component = "Test", url = "/", props = new { name = "Bob" } });
        var ex = Assert.Throws<AssertionException>(() => changed.MatchesSnapshot());

        Assert.Contains("Snapshot mismatch", ex.Message);
        Assert.Contains(methodName, ex.Message);
    }

    [Fact]
    public void UpdateSnapshot_overwrites()
    {
        var methodName = nameof(UpdateSnapshot_overwrites);
        CleanSnapshot(methodName);

        // Create initial snapshot
        var initial = CreateAssertable(new { component = "Test", url = "/", props = new { count = 1 } });
        initial.MatchesSnapshot();

        // Update with new data
        var updated = CreateAssertable(new { component = "Test", url = "/", props = new { count = 2 } });
        updated.UpdateSnapshot();

        // New data should now match
        var verify = CreateAssertable(new { component = "Test", url = "/", props = new { count = 2 } });
        verify.MatchesSnapshot();
    }

    [Fact]
    public void Snapshot_is_indented_json()
    {
        var methodName = nameof(Snapshot_is_indented_json);
        CleanSnapshot(methodName);

        var inertia = CreateAssertable(new { component = "Test", url = "/", props = new { name = "Alice" } });
        inertia.MatchesSnapshot();

        var content = File.ReadAllText(GetSnapshotPath(methodName));
        Assert.Contains("\n", content);
        Assert.Contains("  ", content);
    }

    [Fact]
    public void Is_chainable()
    {
        CleanSnapshot(nameof(Is_chainable));
        var inertia = CreateAssertable(new { component = "Test", url = "/", props = new { name = "Alice" } });

        var result = inertia.MatchesSnapshot();

        Assert.Same(inertia, result);
    }

    private static AssertableInertia CreateAssertable(object page)
    {
        var json = JsonSerializer.Serialize(page);
        var response = new HttpResponseMessage
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
        response.Headers.Add("X-Inertia", "true");
        return AssertableInertia.FromResponseAsync(response).Result;
    }

    private string GetSnapshotPath(string methodName) =>
        Path.Combine(_snapshotDir, $"SnapshotTests.{methodName}.snap");

    private void CleanSnapshot(string methodName)
    {
        var path = GetSnapshotPath(methodName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string GetCallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

    public void Dispose()
    {
        // Clean up all snapshots created by tests
        if (Directory.Exists(_snapshotDir))
        {
            Directory.Delete(_snapshotDir, recursive: true);
        }
    }
}
