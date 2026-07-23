using BSE.Host.Cache;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSE.Host.HealthChecks;

/// <summary>
/// Readiness health check for the distributed cache tier.
/// Returns <see cref="HealthCheckResult.Healthy"/> when Redis is reachable,
/// or <see cref="HealthCheckResult.Degraded"/> when Redis is unavailable
/// (application is running in single-replica MemoryCache fallback mode).
/// <para>
/// HTTP status for <c>Degraded</c> remains <c>200</c> (the ASP.NET Core default),
/// so the load balancer does NOT take the instance out of rotation — it continues
/// to serve traffic, but should not be scaled out.
/// </para>
/// </summary>
public sealed class RedisReadinessCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public RedisReadinessCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var resilientCache = _serviceProvider.GetService<ResilientDistributedCache>();

        if (resilientCache is null)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Redis is not configured. Application is running in single-replica mode " +
                "with in-memory cache. Set Redis__ConnectionString to enable distributed mode."));
        }

        return Task.FromResult(resilientCache.IsUsingFallback
            ? HealthCheckResult.Degraded(
                "Redis is unreachable. Application is running in single-replica degraded mode " +
                "with in-memory fallback cache.")
            : HealthCheckResult.Healthy("Redis distributed cache is reachable."));
    }
}
