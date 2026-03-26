using System.Net.Http.Json;
using System.Text.Json;
using InertiaCore.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InertiaCore.Ssr;

/// <summary>
/// HTTP implementation of <see cref="ISsrGateway"/> that communicates with a Node.js SSR sidecar.
/// </summary>
public sealed partial class HttpSsrGateway : ISsrGateway
{
    private static readonly JsonSerializerOptions s_jsonOptions = Constants.InertiaJsonOptions.CamelCase;

    private readonly HttpClient _httpClient;
    private readonly SsrOptions _ssrOptions;
    private readonly ILogger<HttpSsrGateway> _logger;
    private readonly Action<SsrRenderFailed>? _onRenderFailed;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpSsrGateway"/>.
    /// </summary>
    public HttpSsrGateway(
        HttpClient httpClient,
        IOptions<InertiaOptions> options,
        ILogger<HttpSsrGateway> logger,
        Action<SsrRenderFailed>? onRenderFailed = null)
    {
        _httpClient = httpClient;
        _ssrOptions = options.Value.Ssr;
        _logger = logger;
        _onRenderFailed = onRenderFailed;

        _httpClient.Timeout = TimeSpan.FromSeconds(_ssrOptions.TimeoutSeconds);
    }

    /// <inheritdoc />
    public async Task<SsrResponse?> RenderAsync(
        Dictionary<string, object?> page,
        CancellationToken cancellationToken = default)
    {
        if (!_ssrOptions.Enabled)
        {
            return null;
        }

        var url = $"{_ssrOptions.Url.TrimEnd('/')}/render";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, page, s_jsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return HandleError(SsrErrorType.RenderError,
                    $"SSR render failed with status {response.StatusCode}", page: page);
            }

            var data = await response.Content.ReadFromJsonAsync<SsrResponseData>(s_jsonOptions, cancellationToken);
            if (data == null)
            {
                return HandleError(SsrErrorType.InvalidResponse,
                    "SSR returned null response", page: page);
            }

            return new SsrResponse(
                Head: string.Join("\n", data.Head ?? []),
                Body: data.Body ?? "");
        }
        catch (HttpRequestException ex)
        {
            return HandleError(SsrErrorType.ConnectionRefused,
                $"SSR server unreachable at {url}", ex, page);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return HandleError(SsrErrorType.Timeout,
                $"SSR request timed out after {_ssrOptions.TimeoutSeconds}s", ex, page);
        }
        catch (TaskCanceledException)
        {
            // Request was cancelled by the caller, not a timeout
            return null;
        }
        catch (JsonException ex)
        {
            return HandleError(SsrErrorType.InvalidResponse,
                "SSR returned invalid JSON", ex, page);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!_ssrOptions.Enabled)
        {
            return false;
        }

        try
        {
            var url = $"{_ssrOptions.Url.TrimEnd('/')}/health-check";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private SsrResponse? HandleError(
        SsrErrorType errorType,
        string message,
        Exception? exception = null,
        Dictionary<string, object?>? page = null)
    {
        LogSsrWarning(_logger, message, exception);
        PublishErrorEvent(errorType, message, exception, page);
        ThrowIfConfigured(errorType, message, exception);

        return null;
    }

    private void PublishErrorEvent(
        SsrErrorType errorType, string message, Exception? exception, Dictionary<string, object?>? page)
    {
        _onRenderFailed?.Invoke(new SsrRenderFailed
        {
            ErrorType = errorType,
            Message = message,
            Exception = exception,
            Page = page,
        });
    }

    private void ThrowIfConfigured(SsrErrorType errorType, string message, Exception? exception)
    {
        if (!_ssrOptions.ThrowOnError)
        {
            return;
        }

        throw exception != null
            ? new SsrException(errorType, message, exception)
            : new SsrException(errorType, message);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SSR: {Message}")]
    private static partial void LogSsrWarning(ILogger logger, string message, Exception? exception);

    private sealed record SsrResponseData(string[]? Head, string? Body);
}
