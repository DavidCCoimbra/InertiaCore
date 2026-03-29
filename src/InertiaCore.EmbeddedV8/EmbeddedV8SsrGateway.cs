using System.Text.Json;
using InertiaCore.Configuration;
using InertiaCore.Ssr;
using Microsoft.ClearScript.JavaScript;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InertiaCore.EmbeddedV8;

/// <summary>
/// SSR gateway that renders components in-process using an embedded V8 engine.
/// </summary>
public sealed partial class EmbeddedV8SsrGateway : ISsrGateway
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    private readonly V8EnginePool _pool;
    private readonly SsrOptions _ssrOptions;
    private readonly ILogger<EmbeddedV8SsrGateway> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EmbeddedV8SsrGateway"/>.
    /// </summary>
    public EmbeddedV8SsrGateway(
        V8EnginePool pool,
        IOptions<InertiaOptions> options,
        ILogger<EmbeddedV8SsrGateway> logger)
    {
        _pool = pool;
        _ssrOptions = options.Value.Ssr;
        _logger = logger;
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

        if (!_pool.IsReady)
        {
            LogSsrWarning(_logger, "V8 engine pool not ready yet", null);
            return null;
        }

        var engine = await _pool.LeaseAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Serialize the page to JSON and parse in V8 so JS gets a native object.
            // ClearScript proxies don't support property access on C# dictionaries
            // (page.component would be undefined — need page["component"] instead).
            var pageJson = JsonSerializer.Serialize(page, s_jsonOptions);
            engine.Script.__ssr_page_json = pageJson;

            // Parse the page JSON in V8 so it's a native JS object
            engine.Execute("var __ssr_page = JSON.parse(__ssr_page_json);");

            // Call the render function — returns a JS Promise
            var promise = engine.Evaluate("__inertia_ssr_render(__ssr_page)");

            // Convert JS Promise → C# Task and await the result
            var resultObj = await JavaScriptExtensions.ToTask(promise).ConfigureAwait(false);

            if (resultObj is null || resultObj is Microsoft.ClearScript.Undefined)
            {
                LogSsrWarning(_logger, "V8 render returned no result", null);
                return null;
            }

            // Serialize the result to JSON inside V8 (avoids ClearScript proxy issues)
            engine.Script.__ssr_raw = resultObj;
            var resultJson = (string)engine.Evaluate("JSON.stringify(__ssr_raw)");

            if (string.IsNullOrEmpty(resultJson))
            {
                LogSsrWarning(_logger, "V8 render returned no result", null);
                return null;
            }

            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            var headArray = new List<string>();
            if (root.TryGetProperty("head", out var headEl) && headEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in headEl.EnumerateArray())
                {
                    if (item.GetString() is { } s) headArray.Add(s);
                }
            }

            var body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "";

            return new SsrResponse(
                Head: string.Join("\n", headArray),
                Body: body);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSsrWarning(_logger, "Embedded V8 SSR failed", ex);
            return null;
        }
        finally
        {
            await _pool.ReturnAsync(engine).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_ssrOptions.Enabled && _pool.IsReady);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SSR V8: {Message}")]
    private static partial void LogSsrWarning(ILogger logger, string message, Exception? exception);
}
