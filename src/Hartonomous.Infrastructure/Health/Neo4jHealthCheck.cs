using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var neo4jUri = configuration["Neo4j:Uri"]
            ?? throw new InvalidOperationException("Neo4j:Uri configuration not found.");
        var neo4jUsername = configuration["Neo4j:Username"]
            ?? throw new InvalidOperationException("Neo4j:Username configuration not found.");
        var neo4jPassword = configuration["Neo4j:Password"]
            ?? throw new InvalidOperationException("Neo4j:Password configuration not found.");
        _database = configuration["Neo4j:Database"] ?? "neo4j";

        _driver = GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUsername, neo4jPassword));
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
