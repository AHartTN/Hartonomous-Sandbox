using Hartonomous.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hartonomous.Data.Entities.Entities;

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
        // TODO: Re-implement these using database-centric patterns (DbContext + stored procedures)
        // Previously used deleted repository services
        // services.AddHartonomousPersistence(configuration);      // DbContext + Repositories
        // services.AddHartonomousCoreServices(configuration);     // Serialization, Messaging, Caching, Security, Billing
        // services.AddHartonomousResilience(configuration);       // Circuit breakers, Retry policies, HTTP resilience
        // services.AddHartonomousPipelines(configuration);        // Channel-based async processing + Workers
        // services.AddHartonomousAIServices();                    // Model ingestion, Inference, Generation, Autonomous

        return services;
    }

    /// <summary>
    /// Registers health checks for Hartonomous infrastructure
    /// </summary>
    public static IServiceCollection AddHartonomousHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return Extensions.ObservabilityServiceExtensions.AddHartonomousHealthChecks(services, configuration);
    }

    /// <summary>
    /// Configures PII sanitization and redaction for logging, HTTP logging, and telemetry
    /// </summary>
    public static IServiceCollection AddHartonomousPiiRedaction(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: Re-implement - CoreServiceExtensions was deleted with orphaned repository services
        // return Extensions.CoreServiceExtensions.AddHartonomousPiiRedaction(services, configuration);
        return services;
    }
}
