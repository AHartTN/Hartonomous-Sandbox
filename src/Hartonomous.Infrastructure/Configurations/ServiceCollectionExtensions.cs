using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Provenance;
using Hartonomous.Core.Interfaces.Reasoning;
using Hartonomous.Core.Interfaces.SpatialSearch;
using Hartonomous.Core.Interfaces.Validation;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.Health;
using Hartonomous.Infrastructure.Services.Provenance;
using Hartonomous.Infrastructure.Services.Reasoning;
using Hartonomous.Infrastructure.Services.SpatialSearch;
using Hartonomous.Infrastructure.Services.Validation;
using Hartonomous.Infrastructure.Services.Ingestion;
using Hartonomous.Infrastructure.Services.Ingestion.Strategies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        this IServiceCollection services)
    {
        // Neo4j Driver (Singleton) - uses IOptions for configuration
        services.AddSingleton<IDriver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            
            return GraphDatabase.Driver(
                options.Uri, 
                AuthTokens.Basic(options.Username, options.Password),
                config => config
                    .WithMaxConnectionPoolSize(options.MaxConnectionPoolSize)
                    .WithConnectionTimeout(TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds)));
        });

        // Reasoning services (SQL Server with managed identity)
        services.AddScoped<IReasoningService, SqlReasoningService>();

        // Spatial search services (SQL Server with NetTopologySuite)
        services.AddScoped<ISpatialSearchService, SqlSpatialSearchService>();

        // Provenance query services (Neo4j READ-ONLY)
        services.AddScoped<IProvenanceQueryService, Neo4jProvenanceService>();

        // Validation services
        services.AddScoped<IValidationService, ValidationService>();

        // Content type strategy services (Strategy pattern for OCP compliance)
        services.AddSingleton<IContentTypeStrategyRegistry, ContentTypeStrategyRegistry>();
        services.AddSingleton<IContentTypeStrategy, TextContentTypeStrategy>();
        services.AddSingleton<IContentTypeStrategy, ImageContentTypeStrategy>();
        services.AddSingleton<IContentTypeStrategy, VideoContentTypeStrategy>();
        services.AddSingleton<IContentTypeStrategy, ModelContentTypeStrategy>();

        // Initialize strategy registry with all strategies
        services.AddSingleton<IContentTypeStrategyRegistry>(sp =>
        {
            var registry = new ContentTypeStrategyRegistry();
            var strategies = sp.GetServices<IContentTypeStrategy>();
            foreach (var strategy in strategies)
            {
                registry.RegisterStrategy(strategy);
            }
            return registry;
        });

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
