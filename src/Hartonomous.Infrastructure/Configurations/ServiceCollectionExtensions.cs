using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Atomization;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Core.Interfaces.Billing;
using Hartonomous.Core.Interfaces.Cognition;
using Hartonomous.Core.Interfaces.Concept;
using Hartonomous.Core.Interfaces.Conversation;
using Hartonomous.Core.Interfaces.Discovery;
using Hartonomous.Core.Interfaces.Generation;
using Hartonomous.Core.Interfaces.Inference;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Interfaces.ModelWeight;
using Hartonomous.Core.Interfaces.Models;
using Hartonomous.Core.Interfaces.Ooda;
using Hartonomous.Core.Interfaces.Provenance;
using Hartonomous.Core.Interfaces.Reasoning;
using Hartonomous.Core.Interfaces.Reconstruction;
using Hartonomous.Core.Interfaces.Search;
using Hartonomous.Core.Interfaces.Semantic;
using Hartonomous.Core.Interfaces.SpatialSearch;
using Hartonomous.Core.Interfaces.Stream;
using Hartonomous.Core.Interfaces.Streaming;
using Hartonomous.Core.Interfaces.Validation;
using Hartonomous.Infrastructure.Health;
using Hartonomous.Infrastructure.Services.BackgroundJob;
using Hartonomous.Infrastructure.Services.Cognition;
using Hartonomous.Infrastructure.Services.Conversation;
using Hartonomous.Infrastructure.Services.Discovery;
using Hartonomous.Infrastructure.Services.Generation;
using Hartonomous.Infrastructure.Services.Ingestion;
using Hartonomous.Infrastructure.Services.Ingestion.Strategies;
using Hartonomous.Infrastructure.Services.Models;
using Hartonomous.Infrastructure.Services.Ooda;
using Hartonomous.Infrastructure.Services.Provenance;
using Hartonomous.Infrastructure.Services.Reasoning;
using Hartonomous.Infrastructure.Services.Search;
using Hartonomous.Infrastructure.Services.SpatialSearch;
using Hartonomous.Infrastructure.Services.Streaming;
using Hartonomous.Infrastructure.Services.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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
            
            if (!options.Enabled)
            {
                return null!; // Return null when disabled, health check will handle this
            }
            
            return GraphDatabase.Driver(
                options.Uri, 
                AuthTokens.Basic(options.Username, options.Password),
                config => config
                    .WithMaxConnectionPoolSize(options.MaxConnectionPoolSize)
                    .WithConnectionTimeout(TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds)));
        });

        // Register Neo4j services conditionally
        services.AddScoped<Neo4jProvenanceService>();
        services.AddScoped<MockProvenanceService>();

        // Reasoning services (SQL Server with managed identity)
        services.AddScoped<IReasoningService, SqlReasoningService>();

        // Spatial search services (SQL Server with NetTopologySuite)
        services.AddScoped<ISpatialSearchService, SqlSpatialSearchService>();

        // Provenance query services (Neo4j READ-ONLY) - only register if Neo4j is enabled
        services.AddScoped<IProvenanceQueryService>(sp =>
        {
            var neo4jOptions = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            if (!neo4jOptions.Enabled)
            {
                // Return a mock service when Neo4j is disabled
                return sp.GetRequiredService<MockProvenanceService>();
            }
            return sp.GetRequiredService<Neo4jProvenanceService>();
        });

        // Validation services
        services.AddScoped<IValidationService, ValidationService>();

        // OODA Loop services (autonomous self-optimization)
        services.AddScoped<IOodaService, SqlOodaService>();

        // Search services (semantic, hybrid, fusion, etc.)
        services.AddScoped<ISearchService, SqlSearchService>();

        // Generation services (text, multi-modal, attention)
        services.AddScoped<IGenerationService, SqlGenerationService>();

        // Background job services (replaces in-memory tracking)
        services.AddScoped<IBackgroundJobService, SqlBackgroundJobService>();

        // Provenance write services
        services.AddScoped<IProvenanceWriteService, SqlProvenanceWriteService>();

        // NEW: Conversation services (multi-turn dialogue)
        services.AddScoped<IConversationService, SqlConversationService>();

        // NEW: Cognitive services (activation spreading, spatial projection)
        services.AddScoped<ICognitiveService, SqlCognitiveService>();

        // NEW: Discovery services (concept clustering, unsupervised learning)
        services.AddScoped<IDiscoveryService, SqlDiscoveryService>();

        // NEW: Stream processing services (real-time event orchestration)
        services.AddScoped<IStreamProcessingService, SqlStreamProcessingService>();

        // NEW: Model management services (weight snapshots, versioning)
        services.AddScoped<IModelManagementService, SqlModelManagementService>();

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
            .AddCheck<SqlServerHealthCheck>("sql_server", tags: ["database", "sql"]);

        // Register Neo4j health check conditionally
        services.AddSingleton<Neo4jHealthCheck>(sp =>
        {
            var neo4jOptions = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            return new Neo4jHealthCheck(
                sp.GetRequiredService<ILogger<Neo4jHealthCheck>>(),
                neo4jOptions.Enabled ? sp.GetRequiredService<IDriver>() : null,
                sp.GetRequiredService<IOptions<Neo4jOptions>>()
            );
        });

        services.AddHealthChecks()
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
