using InertiaCore.Constants;

namespace InertiaCore.Testing;

/// <summary>
/// Fluent builder for constructing Inertia partial reload requests in tests.
/// </summary>
public sealed class ReloadRequest
{
    private readonly HttpClient _client;
    private readonly string _url;
    private string? _component;
    private string? _version;
    private string[]? _only;
    private string[]? _except;
    private string[]? _reset;

    private ReloadRequest(HttpClient client, string url)
    {
        _client = client;
        _url = url;
    }

    /// <summary>
    /// Creates a new reload request builder for the given URL.
    /// </summary>
    public static ReloadRequest For(HttpClient client, string url) => new(client, url);

    /// <summary>
    /// Sets the partial component header.
    /// </summary>
    public ReloadRequest Component(string component)
    {
        _component = component;
        return this;
    }

    /// <summary>
    /// Sets the asset version header.
    /// </summary>
    public ReloadRequest Version(string version)
    {
        _version = version;
        return this;
    }

    /// <summary>
    /// Sets props to include in the partial reload.
    /// </summary>
    public ReloadRequest Only(params string[] props)
    {
        if (_except != null)
        {
            throw new InvalidOperationException("Cannot set both Only and Except on a reload request.");
        }

        _only = props;
        return this;
    }

    /// <summary>
    /// Sets props to exclude from the partial reload.
    /// </summary>
    public ReloadRequest Except(params string[] props)
    {
        if (_only != null)
        {
            throw new InvalidOperationException("Cannot set both Only and Except on a reload request.");
        }

        _except = props;
        return this;
    }

    /// <summary>
    /// Sets props to reset (clear merge state) in the partial reload.
    /// </summary>
    public ReloadRequest Reset(params string[] props)
    {
        _reset = props;
        return this;
    }

    /// <summary>
    /// Sends the reload request and returns the raw HTTP response.
    /// </summary>
    public async Task<HttpResponseMessage> SendAsync()
    {
        if (_component == null)
        {
            throw new InvalidOperationException("Component must be set for a partial reload request. Call .Component() first.");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, _url);
        request.Headers.Add(InertiaHeaders.Inertia, "true");
        request.Headers.Add(InertiaHeaders.PartialComponent, _component);

        if (_version != null)
        {
            request.Headers.Add(InertiaHeaders.Version, _version);
        }

        if (_only != null)
        {
            request.Headers.Add(InertiaHeaders.PartialData, string.Join(",", _only));
        }

        if (_except != null)
        {
            request.Headers.Add(InertiaHeaders.PartialExcept, string.Join(",", _except));
        }

        if (_reset != null)
        {
            request.Headers.Add(InertiaHeaders.Reset, string.Join(",", _reset));
        }

        return await _client.SendAsync(request);
    }

    /// <summary>
    /// Sends the reload request and returns an AssertableInertia for fluent assertions.
    /// </summary>
    public async Task<AssertableInertia> SendAndAssertAsync()
    {
        var response = await SendAsync();
        return await AssertableInertia.FromResponseAsync(response);
    }
}
