using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Pipelines;
using Hartonomous.Core.Pipelines.Inference;
using Hartonomous.Core.Pipelines.Ingestion;
using Hartonomous.Core.Resilience;
using Hartonomous.Core.Security;
using Hartonomous.Core.Serialization;
using Hartonomous.Core.Services;
using Hartonomous.Core.Messaging;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Data;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Infrastructure.Services.Enrichment;
using Hartonomous.Infrastructure.Services.Messaging;
using Hartonomous.Infrastructure.Services.Billing;
using Hartonomous.Infrastructure.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        services.Configure<SqlServerOptions>(configuration.GetSection(SqlServerOptions.SectionName));
        services.Configure<MessageBrokerOptions>(configuration.GetSection(MessageBrokerOptions.SectionName));
        services.Configure<CdcOptions>(configuration.GetSection(CdcOptions.SectionName));
        services.Configure<ServiceBrokerResilienceOptions>(configuration.GetSection(ServiceBrokerResilienceOptions.SectionName));
        services.Configure<BillingOptions>(configuration.GetSection(BillingOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));
        services.Configure<AtomGraphOptions>(configuration.GetSection(AtomGraphOptions.SectionName));
        services.PostConfigure<SqlServerOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                options.ConnectionString = connectionString;
            }

            if (options.CommandTimeoutSeconds <= 0)
            {
                options.CommandTimeoutSeconds = configuration.GetValue<int?>($"{SqlServerOptions.SectionName}:CommandTimeoutSeconds") ?? 30;
            }
        });

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

                // Use split queries by default to prevent cartesian explosion
                // Individual queries can override with .AsSingleQuery() if needed
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Enable detailed errors in development
            if (configuration.GetValue<bool>("DetailedErrors", false))
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }

            // Enable query logging in development for performance analysis
            var logLevel = configuration.GetValue<string>("Logging:LogLevel:Microsoft.EntityFrameworkCore");
            if (logLevel == "Debug")
            {
                options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            }
        });

        services.AddSingleton<IJsonSerializer, SystemTextJsonSerializer>();
        services.AddSingleton<ISqlServerConnectionFactory, SqlServerConnectionFactory>();
        services.AddSingleton<ITransientErrorDetector, SqlServerTransientErrorDetector>();
        services.AddSingleton<Func<RetryPolicyOptions, IRetryPolicy>>(sp => options =>
        {
            var detector = sp.GetRequiredService<ITransientErrorDetector>();
            var logger = sp.GetRequiredService<ILogger<ExponentialBackoffRetryPolicy>>();
            return new ExponentialBackoffRetryPolicy(options, detector, logger);
        });
        services.AddSingleton<ICircuitBreakerPolicy>(sp =>
        {
            var resilienceOptions = sp.GetRequiredService<IOptions<ServiceBrokerResilienceOptions>>().Value;
            var options = new CircuitBreakerOptions
            {
                FailureThreshold = resilienceOptions.CircuitBreakerFailureThreshold,
                BreakDuration = resilienceOptions.CircuitBreakerBreakDuration,
                HalfOpenSuccessThreshold = resilienceOptions.CircuitBreakerHalfOpenSuccessThreshold
            };

            return new CircuitBreakerPolicy(options, sp.GetRequiredService<ITransientErrorDetector>(), sp.GetRequiredService<ILogger<CircuitBreakerPolicy>>());
        });
        services.AddSingleton<IServiceBrokerResilienceStrategy, ServiceBrokerResilienceStrategy>();
        services.AddSingleton<IMessageDeadLetterSink, SqlMessageDeadLetterSink>();
        services.AddMemoryCache();

        services.AddSingleton<IAccessPolicyRule, TenantAccessPolicyRule>();
        services.AddSingleton<IAccessPolicyEngine, AccessPolicyEngine>();
        services.AddSingleton<IThrottleEvaluator, InMemoryThrottleEvaluator>();
        services.AddSingleton<IBillingConfigurationProvider, SqlBillingConfigurationProvider>();
        services.AddSingleton<IBillingMeter, UsageBillingMeter>();
        services.AddSingleton<IBillingUsageSink, SqlBillingUsageSink>();
        services.AddScoped<ISqlCommandExecutor, SqlCommandExecutor>();
        services.AddScoped<IAtomGraphWriter, AtomGraphWriter>();
        services.AddSingleton<IMessageBroker, SqlMessageBroker>();

        // Core services (centralized configuration and validation)
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Domain services (Core layer business logic)
        services.AddSingleton<IModelCapabilityService, ModelCapabilityService>();
        services.AddSingleton<IInferenceMetadataService, InferenceMetadataService>();

        // Event services
        services.AddScoped<IEventEnricher, EventEnricher>();

        services.AddScoped<IAtomRepository, AtomRepository>();
        services.AddScoped<IAtomEmbeddingRepository, AtomEmbeddingRepository>();
        services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();
        services.AddScoped<ITensorAtomRepository, TensorAtomRepository>();
        services.AddScoped<IAtomRelationRepository, AtomRelationRepository>();
        services.AddScoped<IIngestionJobRepository, IngestionJobRepository>();
        services.AddScoped<IDeduplicationPolicyRepository, DeduplicationPolicyRepository>();
        services.AddScoped<IAtomIngestionService, AtomIngestionService>();
        services.AddScoped<IModelLayerRepository, ModelLayerRepository>();
    services.AddScoped<ILayerTensorSegmentRepository, LayerTensorSegmentRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ISpatialInferenceService, SpatialInferenceService>();
        services.AddScoped<IStudentModelService, StudentModelService>();
        services.AddScoped<IModelDiscoveryService, ModelDiscoveryService>();
        services.AddScoped<IIngestionStatisticsService, IngestionStatisticsService>();

        services.AddScoped<ModelIngestionProcessor>();
        services.AddScoped<ModelIngestionOrchestrator>();
        services.AddScoped<ModelDownloader>();

        // ============================================================================
        // PIPELINE ARCHITECTURE - MS-Validated Enterprise Patterns
        // ============================================================================

        // OpenTelemetry resources (static instances for pipeline tracing and metrics)
        var activitySource = new ActivitySource("Hartonomous.Pipelines", "1.0.0");
        var meter = new Meter("Hartonomous.Pipelines", "1.0.0");
        services.AddSingleton(activitySource);
        services.AddSingleton(meter);

        // Atom Ingestion Channel (bounded queue with MS-recommended backpressure)
        services.AddSingleton(_ =>
        {
            var capacity = configuration.GetValue("AtomIngestion:QueueCapacity", 1000);
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait, // MS-recommended: blocks producer when full
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };
            return Channel.CreateBounded<AtomIngestionPipelineRequest>(options);
        });

        // Register ChannelReader/Writer for DI
        services.AddSingleton(sp =>
            sp.GetRequiredService<Channel<AtomIngestionPipelineRequest>>().Reader);
        services.AddSingleton(sp =>
            sp.GetRequiredService<Channel<AtomIngestionPipelineRequest>>().Writer);

        // Pipeline factories (scoped for per-request DbContext)
        services.AddScoped<AtomIngestionPipelineFactory>();
        services.AddScoped<EnsembleInferencePipelineFactory>();

        // Inference adapters and repositories
        services.AddScoped<IInferenceOrchestrator, InferenceOrchestratorAdapter>();
        services.AddScoped<IInferenceRequestRepository, InferenceRequestRepository>();

        // Adapter: Allows existing code using IAtomIngestionService to use the pipeline
        services.AddScoped<IAtomIngestionService, AtomIngestionServiceAdapter>();

        // Background worker for async atom ingestion
        services.AddHostedService<AtomIngestionWorker>();

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
