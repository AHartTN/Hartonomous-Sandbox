using System.Diagnostics.Metrics;
using Hartonomous.Core.Configuration;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Lifecycle;
using Hartonomous.Infrastructure.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering observability services (health checks, metrics, telemetry)
/// </summary>
public static class ObservabilityServiceExtensions
{
    /// <summary>
    /// Registers health checks for Hartonomous infrastructure
    /// </summary>
    public static IServiceCollection AddHartonomousHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Database health checks with ready tag (required for readiness probe)
            .AddDbContextCheck<HartonomousDbContext>("db", tags: ["db", "sql", "ready"])

            // Neo4j health check
            .AddCheck<HealthChecks.Neo4jHealthCheck>("neo4j", tags: ["neo4j", "graph", "ready"])

            // Azure Blob Storage health check
            .AddCheck<HealthChecks.AzureBlobStorageHealthCheck>("azure-blob-storage", tags: ["azure", "storage", "ready"])

            // Event Bus health check
            .AddCheck<HealthChecks.EventBusHealthCheck>("event-bus", tags: ["messaging", "ready"])

            // Distributed Cache health check
            .AddCheck<HealthChecks.DistributedCacheHealthCheck>("distributed-cache", tags: ["cache", "ready"]);

        // Register health check implementations
        services.AddSingleton<HealthChecks.Neo4jHealthCheck>();
        services.AddSingleton<HealthChecks.AzureBlobStorageHealthCheck>();
        services.AddSingleton<HealthChecks.EventBusHealthCheck>();
        services.AddSingleton<HealthChecks.DistributedCacheHealthCheck>();

        // Custom metrics for Hartonomous-specific telemetry
        services.AddSingleton<Observability.CustomMetrics>(sp =>
            new Observability.CustomMetrics(sp.GetRequiredService<Meter>()));

        // Rate limiting configuration
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));

        // Graceful shutdown configuration
        services.Configure<GracefulShutdownOptions>(
            configuration.GetSection(GracefulShutdownOptions.SectionName));
        services.AddHostedService<GracefulShutdownService>();

        return services;
    }
}
