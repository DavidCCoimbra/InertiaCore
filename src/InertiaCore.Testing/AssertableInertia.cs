using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using InertiaCore.Constants;

namespace InertiaCore.Testing;

/// <summary>
/// Fluent assertion helper for verifying Inertia responses in tests.
/// </summary>
public sealed partial class AssertableInertia
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// The raw page object as a JSON element.
    /// </summary>
    public JsonElement Page { get; }

    /// <summary>
    /// The component name from the page object.
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// The URL from the page object.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// The version from the page object.
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// The props dictionary from the page object.
    /// </summary>
    public JsonElement Props { get; }

    private AssertableInertia(JsonElement page)
    {
        Page = page;
        Component = page.GetProperty("component").GetString()!;
        Url = page.GetProperty("url").GetString()!;
        Version = page.TryGetProperty("version", out var v) ? v.GetString() : null;
        Props = page.GetProperty("props");
    }

    /// <summary>
    /// Creates an AssertableInertia from an HTTP response. Supports both JSON (Inertia XHR)
    /// and HTML (initial page load with data-page attribute) responses.
    /// </summary>
    public static async Task<AssertableInertia> FromResponseAsync(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        if (contentType.Contains("json"))
        {
            return await FromJsonResponseAsync(response);
        }

        return await FromHtmlResponseAsync(response);
    }

    private static async Task<AssertableInertia> FromJsonResponseAsync(HttpResponseMessage response)
    {
        var page = await response.Content.ReadFromJsonAsync<JsonElement>(s_jsonOptions);
        return new AssertableInertia(page) { Response = response };
    }

    private static async Task<AssertableInertia> FromHtmlResponseAsync(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        var match = DataPagePattern().Match(html);

        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Could not find data-page attribute in HTML response. " +
                "Ensure the response contains an element with a data-page attribute.");
        }

        var json = System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value);
        var page = JsonSerializer.Deserialize<JsonElement>(json);
        return new AssertableInertia(page) { Response = response };
    }

    // -- Fluent assertions --

    /// <summary>
    /// Asserts the component name matches the expected value.
    /// </summary>
    public AssertableInertia HasComponent(string expected)
    {
        if (Component != expected)
        {
            throw new AssertionException($"Expected component '{expected}' but got '{Component}'.");
        }

        return this;
    }

    /// <summary>
    /// Asserts the URL matches the expected value.
    /// </summary>
    public AssertableInertia HasUrl(string expected)
    {
        if (Url != expected)
        {
            throw new AssertionException($"Expected URL '{expected}' but got '{Url}'.");
        }

        return this;
    }

    /// <summary>
    /// Asserts the version matches the expected value.
    /// </summary>
    public AssertableInertia HasVersion(string expected)
    {
        if (Version != expected)
        {
            throw new AssertionException($"Expected version '{expected}' but got '{Version}'.");
        }

        return this;
    }

    /// <summary>
    /// Asserts a prop exists with the expected string value.
    /// </summary>
    public AssertableInertia HasProp(string key, string expected)
    {
        if (!Props.TryGetProperty(key, out var prop))
        {
            throw new AssertionException($"Expected prop '{key}' to exist but it was not found.");
        }

        var actual = prop.GetString();
        if (actual != expected)
        {
            throw new AssertionException($"Expected prop '{key}' to be '{expected}' but got '{actual}'.");
        }

        return this;
    }

    /// <summary>
    /// Asserts a prop exists and passes the deserialized value to a callback for custom assertions.
    /// </summary>
    public AssertableInertia HasProp<T>(string key, Action<T?> assert)
    {
        if (!Props.TryGetProperty(key, out var prop))
        {
            throw new AssertionException($"Expected prop '{key}' to exist but it was not found.");
        }

        var value = prop.Deserialize<T>(s_jsonOptions);
        assert(value);
        return this;
    }

    /// <summary>
    /// Asserts a prop exists with any value.
    /// </summary>
    public AssertableInertia HasProp(string key)
    {
        if (!Props.TryGetProperty(key, out _))
        {
            throw new AssertionException($"Expected prop '{key}' to exist but it was not found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts a prop does not exist.
    /// </summary>
    public AssertableInertia MissingProp(string key)
    {
        if (Props.TryGetProperty(key, out _))
        {
            throw new AssertionException($"Expected prop '{key}' to not exist but it was found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts the page object contains a specific metadata key.
    /// </summary>
    public AssertableInertia HasMetadata(string key)
    {
        if (!Page.TryGetProperty(key, out _))
        {
            throw new AssertionException($"Expected metadata '{key}' to exist but it was not found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts the page object does not contain a specific metadata key.
    /// </summary>
    public AssertableInertia MissingMetadata(string key)
    {
        if (Page.TryGetProperty(key, out _))
        {
            throw new AssertionException($"Expected metadata '{key}' to not exist but it was found.");
        }

        return this;
    }

    // -- Deferred props --

    /// <summary>
    /// Asserts a prop is deferred (appears in deferredProps metadata).
    /// </summary>
    public AssertableInertia HasDeferredProp(string key, string? group = null)
    {
        if (!Page.TryGetProperty("deferredProps", out var deferred))
        {
            throw new AssertionException($"Expected deferred prop '{key}' but no deferredProps metadata found.");
        }

        foreach (var groupEntry in deferred.EnumerateObject())
        {
            foreach (var prop in groupEntry.Value.EnumerateArray())
            {
                if (prop.GetString() != key)
                {
                    continue;
                }

                if (group != null && groupEntry.Name != group)
                {
                    throw new AssertionException(
                        $"Expected deferred prop '{key}' in group '{group}' but found in group '{groupEntry.Name}'.");
                }

                return this;
            }
        }

        throw new AssertionException($"Expected deferred prop '{key}' but it was not found in deferredProps.");
    }

    // -- Merge props --

    /// <summary>
    /// Asserts a prop is merged (appears in mergeProps metadata).
    /// </summary>
    public AssertableInertia HasMergedProp(string key)
    {
        return HasPropInMetadataArray("mergeProps", key);
    }

    /// <summary>
    /// Asserts a prop is deep-merged (appears in deepMergeProps metadata).
    /// </summary>
    public AssertableInertia HasDeepMergedProp(string key)
    {
        return HasPropInMetadataArray("deepMergeProps", key);
    }

    /// <summary>
    /// Asserts a prop is prepended (appears in prependProps metadata).
    /// </summary>
    public AssertableInertia HasPrependedProp(string key)
    {
        return HasPropInMetadataArray("prependProps", key);
    }

    // -- Flash + errors --

    /// <summary>
    /// Asserts flash data exists with the expected key and value.
    /// </summary>
    public AssertableInertia HasFlash(string key, string expected)
    {
        if (!Props.TryGetProperty("flash", out var flash))
        {
            throw new AssertionException("Expected flash data but no 'flash' prop found.");
        }

        if (!flash.TryGetProperty(key, out var value))
        {
            throw new AssertionException($"Expected flash key '{key}' but it was not found.");
        }

        var actual = value.GetString();
        if (actual != expected)
        {
            throw new AssertionException($"Expected flash '{key}' to be '{expected}' but got '{actual}'.");
        }

        return this;
    }

    /// <summary>
    /// Asserts flash data exists with the given key (any value).
    /// </summary>
    public AssertableInertia HasFlash(string key)
    {
        if (!Props.TryGetProperty("flash", out var flash))
        {
            throw new AssertionException("Expected flash data but no 'flash' prop found.");
        }

        if (!flash.TryGetProperty(key, out _))
        {
            throw new AssertionException($"Expected flash key '{key}' but it was not found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts no flash data is present.
    /// </summary>
    public AssertableInertia HasNoFlash()
    {
        if (Props.TryGetProperty("flash", out var flash) && flash.ValueKind == JsonValueKind.Object
            && flash.EnumerateObject().Any())
        {
            throw new AssertionException("Expected no flash data but flash props were found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts a validation error exists for the given field.
    /// </summary>
    public AssertableInertia HasError(string field)
    {
        if (!Props.TryGetProperty("errors", out var errors))
        {
            throw new AssertionException("Expected errors but no 'errors' prop found.");
        }

        if (!errors.TryGetProperty(field, out _))
        {
            throw new AssertionException($"Expected error for field '{field}' but it was not found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts a validation error exists with the expected message.
    /// </summary>
    public AssertableInertia HasError(string field, string expectedMessage)
    {
        if (!Props.TryGetProperty("errors", out var errors))
        {
            throw new AssertionException("Expected errors but no 'errors' prop found.");
        }

        if (!errors.TryGetProperty(field, out var error))
        {
            throw new AssertionException($"Expected error for field '{field}' but it was not found.");
        }

        var actual = error.GetString();
        if (actual != expectedMessage)
        {
            throw new AssertionException(
                $"Expected error for '{field}' to be '{expectedMessage}' but got '{actual}'.");
        }

        return this;
    }

    /// <summary>
    /// Asserts no validation errors are present (errors prop is empty).
    /// </summary>
    public AssertableInertia HasNoErrors()
    {
        if (!Props.TryGetProperty("errors", out var errors))
        {
            return this;
        }

        if (errors.ValueKind == JsonValueKind.Object && errors.EnumerateObject().Any())
        {
            throw new AssertionException("Expected no errors but validation errors were found.");
        }

        return this;
    }

    // -- Shared / Once props --

    /// <summary>
    /// Asserts a prop was shared (appears in sharedProps metadata).
    /// </summary>
    public AssertableInertia HasSharedProp(string key)
    {
        return HasPropInMetadataArray("sharedProps", key);
    }

    /// <summary>
    /// Asserts a prop is a once-resolved prop (appears in onceProps metadata).
    /// </summary>
    public AssertableInertia HasOnceProp(string key)
    {
        if (!Page.TryGetProperty("onceProps", out var once))
        {
            throw new AssertionException($"Expected once prop '{key}' but no onceProps metadata found.");
        }

        if (!once.TryGetProperty(key, out _))
        {
            throw new AssertionException($"Expected once prop '{key}' but it was not found in onceProps.");
        }

        return this;
    }

    /// <summary>
    /// Asserts matchPropsOn metadata contains the expected keys.
    /// </summary>
    public AssertableInertia HasMatchOn(params string[] keys)
    {
        if (!Page.TryGetProperty("matchPropsOn", out var matchOn))
        {
            throw new AssertionException("Expected matchPropsOn metadata but it was not found.");
        }

        foreach (var key in keys)
        {
            if (!matchOn.EnumerateArray().Any(e => e.GetString() == key))
            {
                throw new AssertionException($"Expected '{key}' in matchPropsOn but it was not found.");
            }
        }

        return this;
    }

    // -- History flags --

    /// <summary>
    /// Asserts history encryption is enabled in the page object.
    /// </summary>
    public AssertableInertia HasEncryptedHistory()
    {
        return HasPageFlag("encryptHistory");
    }

    /// <summary>
    /// Asserts clear history flag is set in the page object.
    /// </summary>
    public AssertableInertia HasClearHistory()
    {
        return HasPageFlag("clearHistory");
    }

    /// <summary>
    /// Asserts preserve fragment flag is set in the page object.
    /// </summary>
    public AssertableInertia HasPreservedFragment()
    {
        return HasPageFlag("preserveFragment");
    }

    // -- Generic prop assertion --

    /// <summary>
    /// Asserts a prop exists and satisfies a predicate.
    /// </summary>
    public AssertableInertia Where(string key, Func<JsonElement, bool> predicate)
    {
        if (!Props.TryGetProperty(key, out var prop))
        {
            throw new AssertionException($"Expected prop '{key}' to exist but it was not found.");
        }

        if (!predicate(prop))
        {
            throw new AssertionException($"Prop '{key}' did not satisfy the predicate.");
        }

        return this;
    }

    /// <summary>
    /// Asserts a prop equals the expected typed value.
    /// </summary>
    public AssertableInertia HasPropValue<T>(string key, T expected)
    {
        if (!Props.TryGetProperty(key, out var prop))
        {
            throw new AssertionException($"Expected prop '{key}' to exist but it was not found.");
        }

        var actual = prop.Deserialize<T>(s_jsonOptions);
        if (!Equals(actual, expected))
        {
            throw new AssertionException($"Expected prop '{key}' to be '{expected}' but got '{actual}'.");
        }

        return this;
    }

    // -- Response-level helpers --

    /// <summary>
    /// The original HTTP response (available for custom status/header checks).
    /// </summary>
    public HttpResponseMessage? Response { get; private init; }

    /// <summary>
    /// Asserts the response has the X-Inertia header (is an Inertia JSON response).
    /// </summary>
    public AssertableInertia IsInertiaResponse()
    {
        if (Response == null)
        {
            throw new AssertionException("Response not available. Use FromResponseAsync to preserve the response.");
        }

        if (!Response.Headers.Contains("X-Inertia"))
        {
            throw new AssertionException("Expected X-Inertia header but it was not found.");
        }

        return this;
    }

    /// <summary>
    /// Asserts the response has a 2xx status code.
    /// </summary>
    public AssertableInertia IsSuccessful()
    {
        if (Response == null)
        {
            throw new AssertionException("Response not available.");
        }

        if (!Response.IsSuccessStatusCode)
        {
            throw new AssertionException(
                $"Expected successful response but got {(int)Response.StatusCode}.");
        }

        return this;
    }

    // -- Counts --

    /// <summary>
    /// Asserts the number of props matches the expected count.
    /// </summary>
    public AssertableInertia PropCount(int expected)
    {
        var actual = Props.EnumerateObject().Count();
        if (actual != expected)
        {
            throw new AssertionException($"Expected {expected} props but found {actual}.");
        }

        return this;
    }

    // -- Private helpers --

    private AssertableInertia HasPageFlag(string flag)
    {
        if (!Page.TryGetProperty(flag, out var value) || !value.GetBoolean())
        {
            throw new AssertionException($"Expected '{flag}' to be true in page object.");
        }

        return this;
    }

    private AssertableInertia HasPropInMetadataArray(string metadataKey, string propKey)
    {
        if (!Page.TryGetProperty(metadataKey, out var array))
        {
            throw new AssertionException(
                $"Expected '{propKey}' in {metadataKey} but no {metadataKey} metadata found.");
        }

        if (!array.EnumerateArray().Any(e => e.GetString() == propKey))
        {
            throw new AssertionException(
                $"Expected '{propKey}' in {metadataKey} but it was not found.");
        }

        return this;
    }

    [GeneratedRegex(@"data-page=""([^""]+)""")]
    private static partial Regex DataPagePattern();
}

/// <summary>
/// Exception thrown when an Inertia assertion fails.
/// </summary>
public sealed class AssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance with the specified message.
    /// </summary>
    public AssertionException(string message) : base(message)
    {
    }
}
