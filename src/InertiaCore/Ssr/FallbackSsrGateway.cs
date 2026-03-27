using Microsoft.Extensions.Logging;

namespace InertiaCore.Ssr;

/// <summary>
/// Composite SSR gateway that chains multiple strategies with automatic fallback.
/// If the primary gateway fails, it tries the next one in the chain.
/// Returns null (CSR fallback) if all gateways fail.
/// </summary>
public sealed partial class FallbackSsrGateway : ISsrGateway
{
    private readonly ISsrGateway[] _gateways;
    private readonly ILogger<FallbackSsrGateway> _logger;

    /// <summary>
    /// Initializes a new instance with the given gateway chain (tried in order).
    /// </summary>
    public FallbackSsrGateway(IEnumerable<ISsrGateway> gateways, ILogger<FallbackSsrGateway> logger)
    {
        _gateways = gateways.ToArray();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SsrResponse?> RenderAsync(
        Dictionary<string, object?> page,
        CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < _gateways.Length; i++)
        {
            var gateway = _gateways[i];

            try
            {
                var result = await gateway.RenderAsync(page, cancellationToken);
                if (result is not null)
                {
                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                LogFallback(_logger, gateway.GetType().Name, i + 1, _gateways.Length, ex);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        foreach (var gateway in _gateways)
        {
            if (await gateway.IsHealthyAsync(cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "SSR gateway {GatewayName} failed ({Index}/{Total}), trying next")]
    private static partial void LogFallback(
        ILogger logger, string gatewayName, int index, int total, Exception exception);
}
