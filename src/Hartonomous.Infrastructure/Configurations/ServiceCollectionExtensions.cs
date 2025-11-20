using Hartonomous.Core.Interfaces.Provenance;
using Hartonomous.Core.Interfaces.Reasoning;
using Hartonomous.Core.Interfaces.SpatialSearch;
using Hartonomous.Infrastructure.Health;
using Hartonomous.Infrastructure.Services.Provenance;
using Hartonomous.Infrastructure.Services.Reasoning;
using Hartonomous.Infrastructure.Services.SpatialSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace Hartonomous.Infrastructure.Configurations;

/// <summary>
/// Extension methods for registering Hartonomous infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers production SQL Server and Neo4j implementations for production environments.
    /// </summary>
    public static IServiceCollection AddHartonomousInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Neo4j Driver (Singleton)
        services.AddSingleton<IDriver>(sp =>
        {
            var neo4jUri = configuration["Neo4j:Uri"] ?? "bolt://localhost:7687";
            var neo4jUser = configuration["Neo4j:Username"] ?? "neo4j";
            var neo4jPassword = configuration["Neo4j:Password"] ?? "neo4jneo4j";
            
            return GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUser, neo4jPassword));
        });

        // Reasoning services (SQL Server with managed identity)
        services.AddScoped<IReasoningService, SqlReasoningService>();

        // Spatial search services (SQL Server with NetTopologySuite)
        services.AddScoped<ISpatialSearchService, SqlSpatialSearchService>();

        // Provenance query services (Neo4j READ-ONLY)
        services.AddScoped<IProvenanceQueryService, Neo4jProvenanceService>();

        // Health checks
        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sql_server", tags: ["database", "sql"])
            .AddCheck<Neo4jHealthCheck>("neo4j", tags: ["database", "graph"])
            .AddCheck<KeyVaultHealthCheck>("key_vault", tags: ["azure", "secrets"])
            .AddCheck<AppConfigurationHealthCheck>("app_configuration", tags: ["azure", "config"]);

        return services;
    }

    /// <summary>
    /// Registers mock implementations for marketing site demonstrations.
    /// No database connectivity required.
    /// </summary>
    public static IServiceCollection AddHartonomousMockServices(
        this IServiceCollection services)
    {
        // Mock reasoning services (realistic demo data)
        services.AddScoped<IReasoningService, MockReasoningService>();

        // Mock provenance services (GeoJSON spatial visualization data)
        services.AddScoped<IProvenanceQueryService, MockProvenanceService>();

        return services;
    }
}
