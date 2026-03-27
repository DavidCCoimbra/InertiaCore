using System.Threading.Channels;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InertiaCore.EmbeddedV8;

/// <summary>
/// Pool of pre-warmed V8 engines with the SSR bundle loaded.
/// Each engine handles one render at a time; the pool size determines SSR concurrency.
/// </summary>
public sealed partial class V8EnginePool : IDisposable
{
    private readonly Channel<V8ScriptEngine> _pool;
    private readonly string _bundlePath;
    private readonly int _poolSize;
    private readonly ILogger<V8EnginePool> _logger;
    private volatile bool _isReady;

    /// <summary>
    /// Whether all engines have been warmed and the pool is ready to serve requests.
    /// </summary>
    public bool IsReady => _isReady;

    /// <summary>
    /// Initializes a new instance and starts pre-warming engines on a background thread.
    /// </summary>
    public V8EnginePool(IOptions<EmbeddedSsrOptions> options, ILogger<V8EnginePool> logger)
    {
        _bundlePath = options.Value.BundlePath;
        _poolSize = options.Value.PoolSize;
        _logger = logger;

        _pool = Channel.CreateBounded<V8ScriptEngine>(_poolSize);

        Task.Run(WarmPoolAsync);
    }

    /// <summary>
    /// Lease an engine from the pool. The caller must return it via <see cref="ReturnAsync"/>.
    /// </summary>
    public async Task<V8ScriptEngine> LeaseAsync(CancellationToken ct = default)
    {
        return await _pool.Reader.ReadAsync(ct);
    }

    /// <summary>
    /// Return a leased engine back to the pool.
    /// </summary>
    public async Task ReturnAsync(V8ScriptEngine engine)
    {
        await _pool.Writer.WriteAsync(engine);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _pool.Writer.Complete();
        while (_pool.Reader.TryRead(out var engine))
        {
            engine.Dispose();
        }
    }

    private async Task WarmPoolAsync()
    {
        try
        {
            if (!File.Exists(_bundlePath))
            {
                LogBundleNotFound(_logger, _bundlePath);
                return;
            }

            var bundleSource = await File.ReadAllTextAsync(_bundlePath);

            for (var i = 0; i < _poolSize; i++)
            {
                var engine = CreateEngine(bundleSource);
                await _pool.Writer.WriteAsync(engine);
                LogEngineWarmed(_logger, i + 1, _poolSize);
            }

            _isReady = true;
            LogPoolReady(_logger, _poolSize);
        }
        catch (Exception ex)
        {
            LogWarmFailed(_logger, ex);
        }
    }

    private static V8ScriptEngine CreateEngine(string bundleSource)
    {
        var engine = new V8ScriptEngine(V8ScriptEngineFlags.EnableDynamicModuleImports);

        // Minimal Node.js-like environment shims
        engine.Execute(@"
            var global = this;
            var process = { env: { NODE_ENV: 'production' } };
            var console = {
                log: function() {},
                warn: function() {},
                error: function() {},
            };
        ");

        // Browser API traps — clear errors for SSR-incompatible code
        engine.Execute(@"
            var window = new Proxy({}, {
                get: function(_, prop) {
                    throw new ReferenceError(
                        'window.' + prop + ' is not available during SSR. ' +
                        'Wrap browser API usage in onMounted/useEffect.'
                    );
                }
            });
            var document = new Proxy({}, {
                get: function(_, prop) {
                    throw new ReferenceError(
                        'document.' + prop + ' is not available during SSR. ' +
                        'Wrap DOM access in onMounted/useEffect.'
                    );
                }
            });
            var localStorage = new Proxy({}, {
                get: function() {
                    throw new ReferenceError('localStorage is not available during SSR.');
                }
            });
            var sessionStorage = new Proxy({}, {
                get: function() {
                    throw new ReferenceError('sessionStorage is not available during SSR.');
                }
            });
        ");

        // Load the SSR bundle (same bundle Node.js would run)
        engine.Execute("ssr_bundle", bundleSource);

        return engine;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "SSR bundle not found at {BundlePath}")]
    private static partial void LogBundleNotFound(ILogger logger, string bundlePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "V8 engine {Index}/{Total} warmed")]
    private static partial void LogEngineWarmed(ILogger logger, int index, int total);

    [LoggerMessage(Level = LogLevel.Information, Message = "V8 engine pool ready ({Count} engines)")]
    private static partial void LogPoolReady(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to warm V8 engine pool")]
    private static partial void LogWarmFailed(ILogger logger, Exception exception);
}
