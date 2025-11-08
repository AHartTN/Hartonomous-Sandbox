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
using Hartonomous.Core.Performance;
using Hartonomous.Core.Shared;
using Hartonomous.Data;
using Hartonomous.Data.Repositories;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Resilience;
using Hartonomous.Infrastructure.RateLimiting;
using Hartonomous.Infrastructure.Lifecycle;
using Hartonomous.Infrastructure.ProblemDetails;
using Hartonomous.Infrastructure.Compliance;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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

        services.AddMemoryCache();

        // Distributed Cache - Uses in-memory by default for development
        // For production: Replace with AddStackExchangeRedisCache, AddDistributedSqlServerCache, etc.
        services.AddDistributedMemoryCache();

        // Event Bus - Uses in-memory by default for development
        // For production: Replace with ServiceBusEventBus (Azure Service Bus)
        services.Configure<Messaging.ServiceBusOptions>(configuration.GetSection(Messaging.ServiceBusOptions.SectionName));
        services.AddSingleton<Messaging.IEventBus, Messaging.InMemoryEventBus>();

        // Event handlers (OODA loop + domain events)
        services.AddScoped<Messaging.Handlers.ObservationEventHandler>();
        services.AddScoped<Messaging.Handlers.OrientationEventHandler>();
        services.AddScoped<Messaging.Handlers.DecisionEventHandler>();
        services.AddScoped<Messaging.Handlers.ActionEventHandler>();
        services.AddScoped<Messaging.Handlers.AtomIngestedEventHandler>();
        services.AddScoped<Messaging.Handlers.CacheInvalidatedEventHandler>();
        services.AddScoped<Messaging.Handlers.QuotaExceededEventHandler>();

        // Event bus hosted service - initializes subscriptions on startup
        services.AddHostedService<Messaging.EventBusHostedService>();

        // Resilience Patterns - Circuit breaker, retry, timeout
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
        
        // Default resilience pipeline (circuit breaker + retry + timeout)
        services.AddHttpClient(ResiliencePipelineNames.Default)
            .AddStandardResilienceHandler(options =>
            {
                var resilienceConfig = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();
                
                // Circuit breaker: Open after 5 failures in 30s, break for 60s
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = resilienceConfig.CircuitBreakerSamplingDuration;
                options.CircuitBreaker.MinimumThroughput = resilienceConfig.CircuitBreakerMinimumThroughput;
                options.CircuitBreaker.BreakDuration = resilienceConfig.CircuitBreakerBreakDuration;
                
                // Retry: 5 attempts with exponential backoff (2s, 4s, 8s, 16s, 32s) + jitter
                options.Retry.MaxRetryAttempts = resilienceConfig.RetryMaxAttempts;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = resilienceConfig.RetryUseJitter;
                options.Retry.Delay = resilienceConfig.RetryBaseDelay;
                
                // Timeout: 30s default
                options.TotalRequestTimeout.Timeout = resilienceConfig.DefaultTimeout;
            });

        // Inference resilience pipeline (longer timeout for model inference)
        services.AddHttpClient(ResiliencePipelineNames.Inference)
            .AddStandardResilienceHandler(options =>
            {
                var resilienceConfig = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = resilienceConfig.CircuitBreakerSamplingDuration;
                options.CircuitBreaker.MinimumThroughput = resilienceConfig.CircuitBreakerMinimumThroughput;
                options.CircuitBreaker.BreakDuration = resilienceConfig.CircuitBreakerBreakDuration;
                options.Retry.MaxRetryAttempts = 3; // Fewer retries for long-running operations
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = resilienceConfig.InferenceTimeout; // 5 minutes
            });

        // Generation resilience pipeline (longest timeout for video/audio generation)
        services.AddHttpClient(ResiliencePipelineNames.Generation)
            .AddStandardResilienceHandler(options =>
            {
                var resilienceConfig = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.SamplingDuration = resilienceConfig.CircuitBreakerSamplingDuration;
                options.CircuitBreaker.MinimumThroughput = 5; // Lower threshold for generation
                options.CircuitBreaker.BreakDuration = resilienceConfig.CircuitBreakerBreakDuration;
                options.Retry.MaxRetryAttempts = 2; // Minimal retries for very long operations
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.Delay = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = resilienceConfig.GenerationTimeout; // 10 minutes
            });

        // HttpClient factory for services that need HTTP requests
        services.AddHttpClient();

        // Core services (centralized configuration and validation)
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Domain services (Core layer business logic) - Scoped because they depend on DbContext
        services.AddScoped<IModelCapabilityService, ModelCapabilityService>();
        services.AddScoped<IInferenceMetadataService, InferenceMetadataService>();

        services.AddScoped<IAtomRepository, AtomRepository>();
        services.AddScoped<IAtomEmbeddingRepository, AtomEmbeddingRepository>();
        services.AddScoped<ITensorAtomRepository, TensorAtomRepository>();
        services.AddScoped<ITokenVocabularyRepository, TokenVocabularyRepository>();
        services.AddScoped<IAtomRelationRepository, AtomRelationRepository>();
        services.AddScoped<IIngestionJobRepository, IngestionJobRepository>();
        services.AddScoped<IDeduplicationPolicyRepository, DeduplicationPolicyRepository>();
        services.AddScoped<IModelLayerRepository, ModelLayerRepository>();
        services.AddScoped<ILayerTensorSegmentRepository, LayerTensorSegmentRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();

        // Autonomous learning repositories (EF Core replacements for stored procedures)
        services.AddScoped<IAutonomousAnalysisRepository, AutonomousAnalysisRepository>();
        services.AddScoped<IAutonomousActionRepository, AutonomousActionRepository>();
        services.AddScoped<IAutonomousLearningRepository, AutonomousLearningRepository>();
        services.AddScoped<IConceptDiscoveryRepository, ConceptDiscoveryRepository>();
        services.AddScoped<Hartonomous.Data.Repositories.IVectorSearchRepository, VectorSearchRepository>();

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
        services.AddHealthChecks()
            // Database health checks with ready tag (required for readiness probe)
            .AddDbContextCheck<HartonomousDbContext>("db", tags: ["db", "sql", "ready"])
            
            // Neo4j health check
            .AddCheck<HealthChecks.Neo4jHealthCheck>("neo4j", tags: ["neo4j", "graph", "ready"])
            
            // Azure Blob Storage health check
            .AddCheck<HealthChecks.AzureBlobStorageHealthCheck>("azure-blob-storage", tags: ["azure", "storage", "ready"])
            
            // Event Bus health check
            .AddCheck<HealthChecks.EventBusHealthCheck>("event-bus", tags: ["messaging", "ready"])
            
            // Distributed Cache health check
            .AddCheck<HealthChecks.DistributedCacheHealthCheck>("distributed-cache", tags: ["cache", "ready"]);

        // Register health check implementations
        services.AddSingleton<HealthChecks.Neo4jHealthCheck>();
        services.AddSingleton<HealthChecks.AzureBlobStorageHealthCheck>();
        services.AddSingleton<HealthChecks.EventBusHealthCheck>();
        services.AddSingleton<HealthChecks.DistributedCacheHealthCheck>();

        // Custom metrics for Hartonomous-specific telemetry
        services.AddSingleton<Observability.CustomMetrics>(sp => 
            new Observability.CustomMetrics(sp.GetRequiredService<Meter>()));

        // Enterprise-grade performance monitoring
        services.AddSingleton<PerformanceMonitor>();

        // Rate limiting configuration
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));

        // Graceful shutdown configuration
        services.Configure<Lifecycle.GracefulShutdownOptions>(
            configuration.GetSection(Lifecycle.GracefulShutdownOptions.SectionName));
        services.AddHostedService<Lifecycle.GracefulShutdownService>();

        return services;
    }

    /// <summary>
    /// Configures PII sanitization and redaction for logging, HTTP logging, and telemetry.
    /// Uses Microsoft.Extensions.Compliance.Redaction with custom data classifications.
    /// </summary>
    public static IServiceCollection AddHartonomousPiiRedaction(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<Compliance.PiiSanitizationOptions>(
            configuration.GetSection("PiiSanitization"));

        var options = configuration.GetSection("PiiSanitization").Get<Compliance.PiiSanitizationOptions>() 
            ?? new Compliance.PiiSanitizationOptions();

        // Core redaction services
        services.AddRedaction(redactionBuilder =>
        {
            // Public data - no redaction (we don't register a redactor, so it won't be redacted)
            
            // Private, Personal, and Financial data - mask with asterisks
            // Using custom StarRedactor for visible but anonymized data
            redactionBuilder.SetRedactor<Compliance.StarRedactor>(
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Private),
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Personal),
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Financial));

            // Sensitive and Health data - completely erase using ErasingRedactor
            // This is the fallback redactor, so any data without a specific redactor will be erased
            redactionBuilder.SetRedactor<ErasingRedactor>(
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Sensitive),
                new Microsoft.Extensions.Compliance.Classification.DataClassificationSet(
                    HartonomousDataClassifications.Health));
            
            // Set fallback redactor for any unclassified sensitive data
            redactionBuilder.SetFallbackRedactor<ErasingRedactor>();
        });

        return services;
    }
}