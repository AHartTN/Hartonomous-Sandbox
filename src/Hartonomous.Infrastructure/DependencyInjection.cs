using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hartonomous.Infrastructure;

/// <summary>
/// Extension methods for registering Hartonomous services with DI
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Hartonomous infrastructure services
    /// </summary>
    public static IServiceCollection AddHartonomousInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        var connectionString = configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not found");

        services.AddDbContext<HartonomousDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                
                sqlOptions.CommandTimeout(30);
                
                // Enable spatial types
                sqlOptions.UseNetTopologySuite();
            });

            // Enable detailed errors in development
            if (configuration.GetValue<bool>("DetailedErrors", false))
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        // Register legacy repositories (will be phased out)
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();
        services.AddScoped<IInferenceRepository, InferenceRepository>();
        services.AddScoped<IAtomicPixelRepository, AtomicPixelRepository>();
        services.AddScoped<IAtomicAudioSampleRepository, AtomicAudioSampleRepository>();
        services.AddScoped<IAtomicTextTokenRepository, AtomicTextTokenRepository>();
        services.AddScoped<ITokenVocabularyRepository, TokenVocabularyRepository>();
        services.AddScoped<ICdcRepository, CdcRepository>();

        // Register dimension bucket architecture (NEW)
        services.AddScoped<IWeightRepository<Weight768>, WeightRepository<Weight768>>();
        services.AddScoped<IWeightRepository<Weight1536>, WeightRepository<Weight1536>>();
        services.AddScoped<IWeightRepository<Weight1998>, WeightRepository<Weight1998>>();
        services.AddScoped<IWeightRepository<Weight3996>, WeightRepository<Weight3996>>();
        services.AddScoped<IModelArchitectureService, ModelArchitectureService>();
        services.AddScoped<IWeightCatalogService, WeightCatalogService>();

        // Register legacy services
        services.AddScoped<IModelDiscoveryService, ModelDiscoveryService>();
        services.AddScoped<ModelIngestionOrchestrator>();
        services.AddScoped<IInferenceService, InferenceOrchestrator>();
        services.AddScoped<IUnifiedEmbeddingService, UnifiedEmbeddingService>();

        // Note: Model format readers are registered in ModelIngestion service DI
        // since they are specific to that application, not shared infrastructure

        return services;
    }

    /// <summary>
    /// Registers health checks for Hartonomous infrastructure
    /// </summary>
    public static IServiceCollection AddHartonomousHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("HartonomousDb");

        services.AddHealthChecks()
            .AddDbContextCheck<HartonomousDbContext>("hartonomous-db", tags: ["db", "sql", "hartonomous"]);

        return services;
    }
}
