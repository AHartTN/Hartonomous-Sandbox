using Hartonomous.Core.Configuration;
using Hartonomous.Core.Services;
using Hartonomous.Data.Entities;
using Hartonomous.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        this IServiceCollection services)
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

        // ===== PHASE 2: Business Services (Scoped - share DbContext per request) =====
        services.AddScoped<IIngestionService, IngestionService>();
        
        // TODO: Add more services as they are implemented
        // services.AddScoped<IProvenanceService, Neo4jProvenanceService>();
        // services.AddScoped<IReasoningService, ReasoningService>();
        // services.AddScoped<IAtomQueryService, AtomQueryService>();

        return services;
    }
}
