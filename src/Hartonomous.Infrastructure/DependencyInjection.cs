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

        services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();
        services.AddScoped<IModelLayerRepository, ModelLayerRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ISpatialInferenceService, SpatialInferenceService>();
        services.AddScoped<IStudentModelService, StudentModelService>();
        services.AddScoped<IModelDiscoveryService, ModelDiscoveryService>();

        services.AddScoped<ModelIngestionProcessor>();
        services.AddScoped<ModelIngestionOrchestrator>();

        services.AddScoped<IModelFormatReader<SafetensorsMetadata>, Services.SafetensorsModelReader>();
        services.AddScoped<IModelFormatReader<PyTorchMetadata>, Services.PyTorchModelReader>();
        services.AddScoped<IModelFormatReader<GGUFMetadata>, Services.GGUFModelReader>();

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
