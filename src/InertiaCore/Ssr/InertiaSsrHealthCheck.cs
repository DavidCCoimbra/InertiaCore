using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InertiaCore.Ssr;

/// <summary>
/// Health check that verifies the SSR sidecar is reachable and responding.
/// </summary>
public sealed class InertiaSsrHealthCheck : IHealthCheck
{
    private readonly ISsrGateway _gateway;

    /// <summary>
    /// Initializes a new instance of <see cref="InertiaSsrHealthCheck"/>.
    /// </summary>
    public InertiaSsrHealthCheck(ISsrGateway gateway)
    {
        _gateway = gateway;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = await _gateway.IsHealthyAsync(cancellationToken);

        return isHealthy
            ? HealthCheckResult.Healthy("SSR server is responding")
            : HealthCheckResult.Degraded("SSR server is not responding, falling back to CSR");
    }
}
