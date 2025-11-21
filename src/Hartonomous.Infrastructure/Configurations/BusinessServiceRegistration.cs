using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Atomization;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Core.Interfaces.Billing;
using Hartonomous.Core.Interfaces.Concept;
using Hartonomous.Core.Interfaces.Generation;
using Hartonomous.Core.Interfaces.Inference;
using Hartonomous.Core.Interfaces.Ooda;
using Hartonomous.Core.Interfaces.Provenance;
using Hartonomous.Core.Interfaces.Reasoning;
using Hartonomous.Core.Interfaces.Search;
using Hartonomous.Core.Interfaces.Semantic;
using Hartonomous.Core.Services;
using Hartonomous.Data.Entities;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Infrastructure.Services.Atomization;
using Hartonomous.Infrastructure.Services.Billing;
using Hartonomous.Infrastructure.Services.Concept;
using Hartonomous.Infrastructure.Services.Generation;
using Hartonomous.Infrastructure.Services.Inference;
using Hartonomous.Infrastructure.Services.Ooda;
using Hartonomous.Infrastructure.Services.Reasoning;
using Hartonomous.Infrastructure.Services.Search;
using Hartonomous.Infrastructure.Services.Semantic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Hartonomous.Infrastructure.Configurations;

/// <summary>
/// Service registration for business logic services following Microsoft patterns.
/// Services use DbContext directly (no repository layer).
/// </summary>
public static class BusinessServiceRegistration
{
    /// <summary>
    /// Register business services with correct DI lifetimes.
    /// </summary>
    public static IServiceCollection AddBusinessServices(
        this IServiceCollection services,
        bool includeIngestion = true)
    {
        // ===== PHASE 0.3: DbContext Registration (MUST be Scoped) =====
        services.AddDbContext<HartonomousDbContext>((sp, options) =>
        {
            var databaseOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(databaseOptions.HartonomousDb, sqlOptions =>
            {
                sqlOptions.UseNetTopologySuite();
            });
        }, ServiceLifetime.Scoped); // CRITICAL: Must be Scoped

        // ===== PHASE 1.5: Neo4j Driver (Singleton) =====
        services.AddSingleton<IDriver>(sp =>
        {
            var neo4jOptions = sp.GetRequiredService<IOptions<Neo4jOptions>>().Value;
            
            if (!neo4jOptions.Enabled)
            {
                // Return null driver when disabled - services should handle gracefully
                return null!;
            }
            
            return GraphDatabase.Driver(
                neo4jOptions.Uri,
                AuthTokens.Basic(neo4jOptions.Username, neo4jOptions.Password),
                config => config
                    .WithMaxConnectionPoolSize(neo4jOptions.MaxConnectionPoolSize)
                    .WithConnectionTimeout(TimeSpan.FromSeconds(neo4jOptions.ConnectionTimeoutSeconds)));
        });

        // ===== PHASE 2: Business Services (Scoped - share DbContext per request) =====
        if (includeIngestion)
        {
            services.AddScoped<IIngestionService, IngestionService>();
        }
        
        // ===== PHASE 2: Provenance Services (Neo4j-backed) =====
        services.AddScoped<IProvenanceQueryService, Neo4jProvenanceQueryService>();
        services.AddScoped<ILineageQueryService>(sp => sp.GetRequiredService<IProvenanceQueryService>());
        services.AddScoped<ISessionPathQueryService>(sp => sp.GetRequiredService<IProvenanceQueryService>());
        services.AddScoped<IErrorAnalysisService>(sp => sp.GetRequiredService<IProvenanceQueryService>());
        services.AddScoped<IInfluenceAnalysisService>(sp => sp.GetRequiredService<IProvenanceQueryService>());

        // ===== PHASE 2: Context Retrieval Service (Reasoning) =====
        services.AddScoped<IContextRetrievalService, ContextRetrievalService>();

        // ===== PHASE 3: SQL-backed AI Services =====
        
        // Reasoning Services (Existing)
        services.AddScoped<IReasoningService, SqlReasoningService>();
        
        // Generation Services (Existing)
        services.AddScoped<IGenerationService, SqlGenerationService>();
        
        // OODA Loop Services (Existing)
        services.AddScoped<IOodaService, SqlOodaService>();
        
        // Search Services (Phase 1 - High Priority)
        services.AddScoped<ISearchService, SqlSearchService>();
        
        // Inference Services (Phase 1 - High Priority)
        services.AddScoped<IInferenceService, SqlInferenceService>();
        
        // Concept Services (Phase 2)
        services.AddScoped<IConceptService, SqlConceptService>();
        
        // Semantic Services (Phase 2)
        services.AddScoped<ISemanticService, SqlSemanticService>();

        // ===== PHASE 3: Billing Services (ENTERPRISE GRADE) =====
        services.AddScoped<IBillingService, SqlBillingService>();

        // ===== PHASE 3: Atomization Services (DATA INGESTION) =====
        services.AddScoped<IAtomizationService, SqlAtomizationService>();

        // ===== PHASE 3.5: Background Job Service (JOB QUEUE) =====
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();

        return services;
    }
}
