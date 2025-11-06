using Microsoft.Extensions.Diagnostics.HealthChecks;
using Neo4j.Driver;

namespace Hartonomous.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Neo4j graph database connectivity.
/// </summary>
public class Neo4jHealthCheck : IHealthCheck
{
    private readonly IDriver _driver;

    public Neo4jHealthCheck(IDriver driver)
    {
        _driver = driver;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync("RETURN 1 AS num");
            var record = await result.SingleAsync();
            var num = record["num"].As<int>();

            if (num == 1)
            {
                return HealthCheckResult.Healthy("Neo4j connection successful");
            }

            return HealthCheckResult.Degraded("Neo4j query returned unexpected result");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Neo4j connection failed", ex);
        }
    }
}
