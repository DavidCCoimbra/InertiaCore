using InertiaCore.Configuration;
using InertiaCore.Ssr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InertiaCore.EmbeddedV8;

/// <summary>
/// SSR gateway that renders components in-process using an embedded V8 engine.
/// Zero serialization, zero network — V8 reads C# objects directly via ClearScript proxy.
/// </summary>
public sealed partial class EmbeddedV8SsrGateway : ISsrGateway
{
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

        var engine = await _pool.LeaseAsync(cancellationToken);

        try
        {
            // Pass the C# page object directly to V8 — no serialization.
            // ClearScript creates a transparent proxy. When JS accesses
            // page.props.users, it reads from the C# dictionary directly.
            engine.Script.page = page;

            // Call the render function defined in the SSR bundle
            engine.Execute(@"
                __ssr_result = null;
                __ssr_error = null;
                (async () => {
                    try {
                        __ssr_result = await __inertia_ssr_render(page);
                    } catch (e) {
                        __ssr_error = e.message + '\n' + (e.stack || '');
                    }
                })();
            ");

            var error = engine.Script.__ssr_error as string;
            if (error is not null)
            {
                LogSsrWarning(_logger, $"V8 render error: {error}", null);
                return null;
            }

            var result = engine.Script.__ssr_result;
            var head = ReadStringArray(result.head);
            var body = (string)result.body;

            return new SsrResponse(
                Head: string.Join("\n", head),
                Body: body);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSsrWarning(_logger, "Embedded V8 SSR failed", ex);
            return null;
        }
        finally
        {
            await _pool.ReturnAsync(engine);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_ssrOptions.Enabled && _pool.IsReady);
    }

    private static string[] ReadStringArray(dynamic jsArray)
    {
        var list = new List<string>();
        for (var i = 0; i < (int)jsArray.length; i++)
        {
            list.Add((string)jsArray[i]);
        }

        return list.ToArray();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SSR V8: {Message}")]
    private static partial void LogSsrWarning(ILogger logger, string message, Exception? exception);
}
