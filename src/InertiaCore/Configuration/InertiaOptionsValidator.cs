using Microsoft.Extensions.Options;

namespace InertiaCore.Configuration;

/// <summary>
/// Validates InertiaOptions at startup to catch configuration errors early.
/// </summary>
public sealed class InertiaOptionsValidator : IValidateOptions<InertiaOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, InertiaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RootView))
        {
            return ValidateOptionsResult.Fail("InertiaOptions.RootView must not be empty.");
        }

        if (options.Ssr.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Ssr.Url))
            {
                return ValidateOptionsResult.Fail("SsrOptions.Url must not be empty when SSR is enabled.");
            }

            if (!Uri.TryCreate(options.Ssr.Url, UriKind.Absolute, out _))
            {
                return ValidateOptionsResult.Fail($"SsrOptions.Url '{options.Ssr.Url}' is not a valid URI.");
            }
        }

        if (options.Ssr.TimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("SsrOptions.TimeoutSeconds must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
