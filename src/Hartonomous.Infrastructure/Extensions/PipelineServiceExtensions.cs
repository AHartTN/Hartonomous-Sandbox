using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Performance;
using Hartonomous.Core.Pipelines;
using Hartonomous.Core.Pipelines.Inference;
using Hartonomous.Core.Pipelines.Ingestion;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Infrastructure.Services.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering pipeline architecture services (channels, workers, factories)
/// </summary>
public static class PipelineServiceExtensions
{
    /// <summary>
    /// Registers MS-validated enterprise pipeline patterns (Channel-based async processing)
    /// </summary>
    public static IServiceCollection AddHartonomousPipelines(
        this IServiceCollection services,
        IConfiguration configuration)
    {
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

        // Inference adapters and orchestrators
        services.AddScoped<IInferenceOrchestrator, InferenceOrchestratorAdapter>();

        // Adapter: Allows existing code using IAtomIngestionService to use the pipeline
        services.AddScoped<IAtomIngestionService, AtomIngestionServiceAdapter>();

        // Background workers for async processing
        services.AddHostedService<AtomIngestionWorker>();
        services.AddHostedService<InferenceJobWorker>();

        // Enterprise-grade performance monitoring
        services.AddSingleton<PerformanceMonitor>();

        return services;
    }
}
