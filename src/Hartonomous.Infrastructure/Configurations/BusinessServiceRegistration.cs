using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Provenance;
using Hartonomous.Core.Interfaces.Reasoning;
using Hartonomous.Core.Services;
using Hartonomous.Data.Entities;
using Hartonomous.Infrastructure.Services;
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

        return services;
    }
}
