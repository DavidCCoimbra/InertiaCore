using InertiaCore.Vite.Configuration;
using InertiaCore.Vite.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InertiaCore.Vite.Tests;

[Trait("Class", "ViteDevServerDetector")]
public class ViteDevServerDetectorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _webRoot;

    public ViteDevServerDetectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"vite-test-{Guid.NewGuid():N}");
        _webRoot = Path.Combine(_tempDir, "wwwroot");
        Directory.CreateDirectory(_webRoot);
    }

    [Fact]
    public void Returns_true_when_hot_file_exists()
    {
        WriteHotFile("http://localhost:5173");

        var detector = CreateDetector();

        Assert.True(detector.IsRunning());
    }

    [Fact]
    public void Returns_false_when_hot_file_missing()
    {
        var detector = CreateDetector();

        Assert.False(detector.IsRunning());
    }

    [Fact]
    public void Returns_false_in_production()
    {
        WriteHotFile("http://localhost:5173");

        var detector = CreateDetector(isDevelopment: false);

        Assert.False(detector.IsRunning());
    }

    [Fact]
    public void Returns_dev_server_url()
    {
        WriteHotFile("http://localhost:5173/build");

        var detector = CreateDetector();
        detector.IsRunning(); // populate cache

        Assert.Equal("http://localhost:5173/build", detector.GetUrl());
    }

    [Fact]
    public void Trims_whitespace_from_url()
    {
        WriteHotFile("  http://localhost:5173  \n");

        var detector = CreateDetector();
        detector.IsRunning();

        Assert.Equal("http://localhost:5173", detector.GetUrl());
    }

    [Fact]
    public void Throws_when_not_running()
    {
        var detector = CreateDetector();

        Assert.Throws<InvalidOperationException>(() => detector.GetUrl());
    }

    [Fact]
    public void Returns_false_after_cache_expires_and_hot_file_removed()
    {
        WriteHotFile("http://localhost:5173");

        var detector = CreateDetector();

        Assert.True(detector.IsRunning());

        // Remove hot file
        File.Delete(Path.Combine(_webRoot, "hot"));

        // Create a fresh detector (no cache) to simulate cache expiry
        var freshDetector = CreateDetector();
        Assert.False(freshDetector.IsRunning());
    }

    [Fact]
    public void Returns_true_from_cache_on_second_call()
    {
        WriteHotFile("http://localhost:5173");
        var detector = CreateDetector();

        Assert.True(detector.IsRunning());
        // Second call within 2s cache window — should still be true without re-reading file
        Assert.True(detector.IsRunning());
    }

    [Fact]
    public void Handles_empty_hot_file()
    {
        WriteHotFile("");
        var detector = CreateDetector();

        // IsRunning reads the file, gets empty string
        Assert.True(detector.IsRunning());
        // GetUrl returns empty trimmed string
        Assert.Equal("", detector.GetUrl());
    }

    private ViteDevServerDetector CreateDetector(
        bool isDevelopment = true)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(_webRoot);
        env.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);

        var options = Options.Create(new ViteOptions());
        return new ViteDevServerDetector(env, options);
    }

    private void WriteHotFile(string content)
    {
        File.WriteAllText(Path.Combine(_webRoot, "hot"), content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
