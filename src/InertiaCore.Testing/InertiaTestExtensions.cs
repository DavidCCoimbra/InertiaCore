using InertiaCore.Constants;

namespace InertiaCore.Testing;

/// <summary>
/// Extension methods for HttpClient and HttpResponseMessage to simplify Inertia testing.
/// </summary>
public static class InertiaTestExtensions
{
    /// <summary>
    /// Sends a GET request with Inertia headers. Returns a JSON Inertia response.
    /// </summary>
    public static async Task<HttpResponseMessage> GetInertiaAsync(
        this HttpClient client, string url, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(InertiaHeaders.Inertia, "true");

        if (version != null)
        {
            request.Headers.Add(InertiaHeaders.Version, version);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a POST request with Inertia headers.
    /// </summary>
    public static async Task<HttpResponseMessage> PostInertiaAsync(
        this HttpClient client, string url, HttpContent? content = null, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Add(InertiaHeaders.Inertia, "true");

        if (version != null)
        {
            request.Headers.Add(InertiaHeaders.Version, version);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a PUT request with Inertia headers.
    /// </summary>
    public static async Task<HttpResponseMessage> PutInertiaAsync(
        this HttpClient client, string url, HttpContent? content = null, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        request.Headers.Add(InertiaHeaders.Inertia, "true");

        if (version != null)
        {
            request.Headers.Add(InertiaHeaders.Version, version);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a DELETE request with Inertia headers.
    /// </summary>
    public static async Task<HttpResponseMessage> DeleteInertiaAsync(
        this HttpClient client, string url, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add(InertiaHeaders.Inertia, "true");

        if (version != null)
        {
            request.Headers.Add(InertiaHeaders.Version, version);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a partial reload request with X-Inertia-Partial-Data (only) header.
    /// </summary>
    public static async Task<HttpResponseMessage> PartialReloadAsync(
        this HttpClient client, string url, string component, string[] only, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.PartialComponent, component);
        request.Headers.Add(InertiaHeaders.PartialData, string.Join(",", only));

        if (version != null)
        {
            request.Headers.Add(InertiaHeaders.Version, version);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Sends a partial reload request with X-Inertia-Partial-Except header.
    /// </summary>
    public static async Task<HttpResponseMessage> PartialReloadExceptAsync(
        this HttpClient client, string url, string component, string[] except, string? version = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.PartialComponent, component);
        request.Headers.Add(InertiaHeaders.PartialExcept, string.Join(",", except));

        if (version != null)
        {
            request.Headers.Add(InertiaHeaders.Version, version);
        }

        return await client.SendAsync(request);
    }

    /// <summary>
    /// Extracts an AssertableInertia from an HTTP response for fluent assertions.
    /// </summary>
    public static async Task<AssertableInertia> AssertInertiaAsync(this HttpResponseMessage response)
    {
        return await AssertableInertia.FromResponseAsync(response);
    }

    /// <summary>
    /// Sends a GET Inertia request and returns an AssertableInertia for fluent assertions.
    /// </summary>
    public static async Task<AssertableInertia> GetInertiaAssertAsync(
        this HttpClient client, string url, string? version = null)
    {
        var response = await client.GetInertiaAsync(url, version);
        return await response.AssertInertiaAsync();
    }
}
