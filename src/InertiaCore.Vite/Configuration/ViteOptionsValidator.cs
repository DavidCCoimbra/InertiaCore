using Microsoft.Extensions.Options;

namespace InertiaCore.Vite.Configuration;

/// <summary>
/// Validates ViteOptions at startup to catch misconfigurations early.
/// </summary>
public sealed class ViteOptionsValidator : IValidateOptions<ViteOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ViteOptions options)
    {
        if (options.EntryPoints.Length == 0)
        {
            return ValidateOptionsResult.Fail("EntryPoints must contain at least one entry point.");
        }

        if (string.IsNullOrWhiteSpace(options.BuildDirectory))
        {
            return ValidateOptionsResult.Fail("BuildDirectory cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.ManifestPath))
        {
            return ValidateOptionsResult.Fail("ManifestPath cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.HotFilePath))
        {
            return ValidateOptionsResult.Fail("HotFilePath cannot be empty.");
        }

        return ValidateOptionsResult.Success;
    }
}
