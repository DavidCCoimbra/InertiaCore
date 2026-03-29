using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InertiaCore.EmbeddedV8;

/// <summary>
/// Pool of pre-warmed V8 engines with the SSR bundle loaded.
/// Each engine handles one render at a time; the pool size determines SSR concurrency.
/// Call <see cref="TriggerReloadAsync"/> to hot-swap engines with an updated bundle.
/// </summary>
public sealed partial class V8EnginePool : IDisposable
{
    private readonly string _bundlePath;
    private readonly int _poolSize;
    private readonly ILogger<V8EnginePool> _logger;

    private volatile EngineSet _current = null!;
    private volatile bool _isReady;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    /// <summary>
    /// Whether the pool is ready to serve requests.
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

        Task.Run(InitializeAsync);
    }

    /// <summary>
    /// Lease an engine from the pool. The caller must return it via <see cref="ReturnAsync"/>.
    /// </summary>
    public async Task<V8ScriptEngine> LeaseAsync(CancellationToken ct = default)
    {
        return await _current.Channel.Reader.ReadAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Return a leased engine back to the pool.
    /// </summary>
    public async Task ReturnAsync(V8ScriptEngine engine)
    {
        var current = _current;

        // If the engine belongs to a stale pool (after hot-swap), dispose it instead
        if (current.Contains(engine))
        {
            await current.Channel.Writer.WriteAsync(engine).ConfigureAwait(false);
        }
        else
        {
            engine.Dispose();
        }
    }

    /// <summary>
    /// Reloads the SSR bundle from disk and hot-swaps all engines.
    /// Call this from a post-build signal (e.g., the Vite plugin's reloadUrl).
    /// </summary>
    public async Task TriggerReloadAsync()
    {
        if (!await _reloadLock.WaitAsync(0).ConfigureAwait(false)) return;

        try
        {
            LogBundleChanged(_logger, _poolSize);
            var sw = Stopwatch.StartNew();

            var bundleSource = await ReadBundleWithRetryAsync().ConfigureAwait(false);
            if (bundleSource == null) return;

            var newSet = await WarmFromSourceAsync(bundleSource).ConfigureAwait(false);
            if (newSet == null) return;

            var oldSet = Interlocked.Exchange(ref _current, newSet);

            LogBundleReloaded(_logger, sw.ElapsedMilliseconds);

            // Dispose old engines after a delay to let in-flight renders complete
            _ = DisposeAfterDelay(oldSet, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            LogReloadFailed(_logger, ex);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _reloadLock.Dispose();
        _current?.Dispose();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var engineSet = await WarmPoolAsync().ConfigureAwait(false);
            if (engineSet == null) return;

            _current = engineSet;
            _isReady = true;
        }
        catch (Exception ex)
        {
            LogWarmFailed(_logger, ex);
        }
    }

    private async Task<EngineSet?> WarmPoolAsync()
    {
        if (!File.Exists(_bundlePath))
        {
            LogBundleNotFound(_logger, _bundlePath);
            return null;
        }

        var bundleSource = await File.ReadAllTextAsync(_bundlePath).ConfigureAwait(false);
        return await WarmFromSourceAsync(bundleSource).ConfigureAwait(false);
    }

    private async Task<EngineSet?> WarmFromSourceAsync(string bundleSource)
    {
        try
        {
            var channel = Channel.CreateBounded<V8ScriptEngine>(_poolSize);
            var engines = new HashSet<V8ScriptEngine>(ReferenceEqualityComparer.Instance);

            for (var i = 0; i < _poolSize; i++)
            {
                var engine = CreateEngine(bundleSource);
                engines.Add(engine);
                await channel.Writer.WriteAsync(engine).ConfigureAwait(false);
                LogEngineWarmed(_logger, i + 1, _poolSize);
            }

            LogPoolReady(_logger, _poolSize);
            return new EngineSet(channel, engines);
        }
        catch (Exception ex)
        {
            LogWarmFailed(_logger, ex);
            return null;
        }
    }

    private async Task<string?> ReadBundleWithRetryAsync(int maxRetries = 5, int delayMs = 200)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await File.ReadAllTextAsync(_bundlePath).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // File still being written — wait and retry
                await Task.Delay(delayMs * (i + 1)).ConfigureAwait(false);
            }
        }

        LogBundleNotFound(_logger, _bundlePath);
        return null;
    }

    private static async Task DisposeAfterDelay(EngineSet set, TimeSpan delay)
    {
        await Task.Delay(delay).ConfigureAwait(false);
        try
        {
            set.Dispose();
        }
        catch
        {
            // V8 engine disposal can throw if the engine is in a bad state.
            // Swallow to prevent unobserved task exceptions.
        }
    }

    private static V8ScriptEngine CreateEngine(string bundleSource)
    {
        var engine = new V8ScriptEngine(
            V8ScriptEngineFlags.EnableDynamicModuleImports |
            V8ScriptEngineFlags.EnableTaskPromiseConversion);

        // Browser API shims — minimal stubs for SSR
        engine.Execute(@"
            var global = this;
            var process = { env: { NODE_ENV: 'production' } };
            var console = { log: function(){}, warn: function(){}, error: function(){}, info: function(){}, debug: function(){} };
            var navigator = { userAgent: 'InertiaCore-V8-SSR' };
            var location = { href: '', hostname: '', pathname: '/', protocol: 'https:', search: '', hash: '' };
            var history = { scrollRestoration: 'auto', pushState: function(){}, replaceState: function(){}, back: function(){}, forward: function(){}, go: function(){}, state: null };
            var document = { createElement: function(){ return { setAttribute: function(){}, style: {} }; }, createTextNode: function(){ return {}; }, querySelector: function(){ return null; }, querySelectorAll: function(){ return []; }, getElementById: function(){ return null; }, addEventListener: function(){}, removeEventListener: function(){}, head: { appendChild: function(){} }, body: { appendChild: function(){} }, documentElement: { style: {} } };
            var localStorage = { getItem: function(){ return null; }, setItem: function(){}, removeItem: function(){} };
            var sessionStorage = { getItem: function(){ return null; }, setItem: function(){}, removeItem: function(){} };
            function HTMLElement() {} function HTMLDivElement() {} function HTMLAnchorElement() {} function SVGElement() {} function Text() {}
            var self = globalThis;
            if (typeof setTimeout === 'undefined') { globalThis.setTimeout = function(fn, ms) { if (typeof fn === 'function') fn(); return 0; }; }
            if (typeof clearTimeout === 'undefined') { globalThis.clearTimeout = function() {}; }
            if (typeof setInterval === 'undefined') { globalThis.setInterval = function() { return 0; }; }
            if (typeof clearInterval === 'undefined') { globalThis.clearInterval = function() {}; }
            if (typeof setImmediate === 'undefined') { globalThis.setImmediate = function(fn) { if (typeof fn === 'function') fn(); return 0; }; }
            if (typeof queueMicrotask === 'undefined') { globalThis.queueMicrotask = function(fn) { Promise.resolve().then(fn); }; }
            var _b64 = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=';
            function atob(i){var s=String(i).replace(/[=]+$/,''),o='';for(var bc=0,bs,b,idx=0;b=s.charAt(idx++);~b&&(bs=bc%4?bs*64+b:b,bc++%4)?o+=String.fromCharCode(255&bs>>(-2*bc&6)):0){b=_b64.indexOf(b);}return o;}
            function btoa(i){var s=String(i),o='';for(var bl,ch,idx=0,m=_b64;s.charAt(idx|0)||(m='=',idx%1);o+=m.charAt(63&bl>>8-idx%1*8)){ch=s.charCodeAt(idx+=3/4);bl=bl<<8|ch;}return o;}
            var Buffer = { from: function(s){ return { toString: function(){ return atob(s); } }; } };
        ");

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

    [LoggerMessage(Level = LogLevel.Information, Message = "SSR bundle changed, reloading {PoolSize} engines...")]
    private static partial void LogBundleChanged(ILogger logger, int poolSize);

    [LoggerMessage(Level = LogLevel.Information, Message = "SSR bundle reloaded in {ElapsedMs}ms")]
    private static partial void LogBundleReloaded(ILogger logger, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to reload SSR bundle")]
    private static partial void LogReloadFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Holds a channel of engines and tracks which engines belong to this set.
    /// </summary>
    private sealed class EngineSet : IDisposable
    {
        private readonly HashSet<V8ScriptEngine> _engines;

        public Channel<V8ScriptEngine> Channel { get; }

        public EngineSet(Channel<V8ScriptEngine> channel, HashSet<V8ScriptEngine> engines)
        {
            Channel = channel;
            _engines = engines;
        }

        public bool Contains(V8ScriptEngine engine) => _engines.Contains(engine);

        public void Dispose()
        {
            Channel.Writer.Complete();
            foreach (var engine in _engines)
            {
                engine.Dispose();
            }
        }
    }
}
