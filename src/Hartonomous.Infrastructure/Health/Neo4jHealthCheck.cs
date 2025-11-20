using Hartonomous.Core.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Hartonomous.Infrastructure.Health;

/// <summary>
/// Health check for Neo4j connectivity and responsiveness.
/// </summary>
public sealed class Neo4jHealthCheck : IHealthCheck
{
    private readonly ILogger<Neo4jHealthCheck> _logger;
    private readonly IDriver _driver;
    private readonly string _database;

    public Neo4jHealthCheck(
        ILogger<Neo4jHealthCheck> logger,
        IDriver driver,
        IOptions<Neo4jOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        
        var neo4jOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _database = neo4jOptions.Database;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var session = _driver.AsyncSession(o => o
                .WithDatabase(_database)
                .WithDefaultAccessMode(AccessMode.Read));

            // Simple query to verify connectivity
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("RETURN 1 as value");
                var record = await cursor.SingleAsync();
                return record["value"].As<int>();
            });

            if (result == 1)
            {
                _logger.LogDebug("Neo4j health check passed.");
                return HealthCheckResult.Healthy("Neo4j is responsive.");
            }

            return HealthCheckResult.Unhealthy("Neo4j query returned unexpected result.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo4j health check failed.");
            return HealthCheckResult.Unhealthy("Neo4j connection failed.", ex);
        }
    }
}
