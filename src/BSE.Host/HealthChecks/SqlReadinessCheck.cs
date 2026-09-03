using BSE.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BSE.Host.HealthChecks;

/// <summary>
/// Readiness health check: attempts to open a SQL Server connection.
/// Returns Healthy when a connection can be established; Unhealthy otherwise.
/// Mapped to /health/ready — a 503 is acceptable when the database is unreachable.
/// </summary>
public sealed class SqlReadinessCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SqlReadinessCheck(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            return Task.FromResult(HealthCheckResult.Healthy("SQL Server connection established."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("SQL Server connection failed.", ex));
        }
    }
}
