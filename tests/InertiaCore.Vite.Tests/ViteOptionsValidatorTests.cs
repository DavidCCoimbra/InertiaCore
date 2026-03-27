using InertiaCore.Vite.Configuration;

namespace InertiaCore.Vite.Tests;

[Trait("Class", "ViteOptionsValidator")]
public class ViteOptionsValidatorTests
{
    private readonly ViteOptionsValidator _validator = new();

    [Fact]
    public void Valid_options_pass()
    {
        var result = _validator.Validate(null, new ViteOptions());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Empty_entry_points_fails()
    {
        var result = _validator.Validate(null, new ViteOptions { EntryPoints = [] });

        Assert.True(result.Failed);
        Assert.Contains("EntryPoints", result.FailureMessage);
    }

    [Fact]
    public void Empty_build_directory_fails()
    {
        var result = _validator.Validate(null, new ViteOptions { BuildDirectory = "" });

        Assert.True(result.Failed);
        Assert.Contains("BuildDirectory", result.FailureMessage);
    }

    [Fact]
    public void Whitespace_build_directory_fails()
    {
        var result = _validator.Validate(null, new ViteOptions { BuildDirectory = "  " });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Empty_manifest_path_fails()
    {
        var result = _validator.Validate(null, new ViteOptions { ManifestPath = "" });

        Assert.True(result.Failed);
        Assert.Contains("ManifestPath", result.FailureMessage);
    }

    [Fact]
    public void Empty_hot_file_path_fails()
    {
        var result = _validator.Validate(null, new ViteOptions { HotFilePath = "" });

        Assert.True(result.Failed);
        Assert.Contains("HotFilePath", result.FailureMessage);
    }
}
