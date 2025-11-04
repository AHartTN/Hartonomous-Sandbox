using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// Background worker that processes atom ingestion requests using System.Threading.Channels.
/// Implements producer-consumer pattern with backpressure handling and observability.
/// </summary>
public sealed class AtomIngestionWorker : BackgroundService
{
    private readonly Channel<AtomIngestionPipelineRequest> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AtomIngestionWorker> _logger;
    private readonly ActivitySource? _activitySource;
    private readonly Meter? _meter;

    // Metrics
    private readonly Counter<long>? _requestsProcessedCounter;
    private readonly Counter<long>? _requestsFailedCounter;
    private readonly Histogram<double>? _processingDurationHistogram;
    private readonly ObservableGauge<int>? _queueDepthGauge;

    private int _currentQueueDepth;

    public AtomIngestionWorker(
        Channel<AtomIngestionPipelineRequest> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<AtomIngestionWorker> logger,
        ActivitySource? activitySource = null,
        Meter? meter = null)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = activitySource;
        _meter = meter;

        if (_meter != null)
        {
            _requestsProcessedCounter = _meter.CreateCounter<long>(
                "atom_ingestion.requests_processed",
                description: "Total number of atom ingestion requests processed");

            _requestsFailedCounter = _meter.CreateCounter<long>(
                "atom_ingestion.requests_failed",
                description: "Total number of atom ingestion requests that failed");

            _processingDurationHistogram = _meter.CreateHistogram<double>(
                "atom_ingestion.processing_duration",
                unit: "ms",
                description: "Duration of atom ingestion request processing");

            _queueDepthGauge = _meter.CreateObservableGauge(
                "atom_ingestion.queue_depth",
                () => _currentQueueDepth,
                description: "Current depth of the atom ingestion queue");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Atom ingestion worker starting...");

        try
        {
            // Read all requests from the channel until cancellation
            await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                Interlocked.Decrement(ref _currentQueueDepth);

                var startTime = DateTime.UtcNow;
                using var activity = _activitySource?.StartActivity("atom-ingestion-worker-process");
                activity?.SetTag("worker.queue_depth", _currentQueueDepth);

                try
                {
                    // Create new scope for dependency injection
                    using var scope = _scopeFactory.CreateScope();
                    var pipelineFactory = scope.ServiceProvider.GetRequiredService<AtomIngestionPipelineFactory>();

                    // Execute pipeline
                    var result = await pipelineFactory.IngestAtomAsync(request, stoppingToken);

                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    // Record success metrics
                    _requestsProcessedCounter?.Add(1, new KeyValuePair<string, object?>("status", "success"));
                    _processingDurationHistogram?.Record(duration);

                    activity?.SetTag("atom.id", result.Atom.AtomId);
                    activity?.SetTag("atom.duplicate", result.WasDuplicate);
                    activity?.SetTag("processing.duration_ms", duration);
                    activity?.SetStatus(ActivityStatusCode.Ok);

                    _logger.LogDebug(
                        "Processed atom ingestion. AtomId: {AtomId}, WasDuplicate: {WasDuplicate}, Duration: {Duration}ms",
                        result.Atom.AtomId,
                        result.WasDuplicate,
                        duration);
                }
                catch (Exception ex)
                {
                    var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    // Record failure metrics
                    _requestsFailedCounter?.Add(1, new KeyValuePair<string, object?>("error_type", ex.GetType().Name));
                    _processingDurationHistogram?.Record(duration);

                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity?.RecordException(ex);

                    _logger.LogError(
                        ex,
                        "Failed to process atom ingestion. Modality: {Modality}, Duration: {Duration}ms",
                        request.Modality,
                        duration);

                    // TODO: Implement dead letter queue for failed requests
                    // await SendToDeadLetterQueueAsync(request, ex, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Atom ingestion worker stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Atom ingestion worker encountered fatal error.");
            throw;
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Atom ingestion worker stopping...");

        // Signal completion to channel (no more writes)
        _channel.Writer.Complete();

        return base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Producer service for enqueueing atom ingestion requests.
/// Demonstrates bounded channel with backpressure handling.
/// </summary>
public sealed class AtomIngestionProducer
{
    private readonly Channel<AtomIngestionPipelineRequest> _channel;
    private readonly ILogger<AtomIngestionProducer> _logger;
    private int _queueDepth;

    public AtomIngestionProducer(
        Channel<AtomIngestionPipelineRequest> channel,
        ILogger<AtomIngestionProducer> logger)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Enqueues a request for background processing.
    /// </summary>
    /// <param name="request">The ingestion request to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if enqueued successfully, false if channel is full and strategy is to drop.</returns>
    public async Task<bool> EnqueueAsync(
        AtomIngestionPipelineRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _channel.Writer.WriteAsync(request, cancellationToken);
            Interlocked.Increment(ref _queueDepth);

            _logger.LogTrace(
                "Enqueued atom ingestion request. Modality: {Modality}, QueueDepth: {QueueDepth}",
                request.Modality,
                _queueDepth);

            return true;
        }
        catch (ChannelClosedException)
        {
            _logger.LogWarning("Attempted to enqueue request but channel is closed.");
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Enqueue operation cancelled.");
            return false;
        }
    }

    /// <summary>
    /// Streams multiple requests for background processing.
    /// Demonstrates IAsyncEnumerable integration.
    /// </summary>
    public async Task EnqueueManyAsync(
        IAsyncEnumerable<AtomIngestionPipelineRequest> requests,
        CancellationToken cancellationToken = default)
    {
        var count = 0;

        await foreach (var request in requests.WithCancellation(cancellationToken))
        {
            await EnqueueAsync(request, cancellationToken);
            count++;
        }

        _logger.LogInformation("Enqueued {Count} atom ingestion requests.", count);
    }

    /// <summary>
    /// Gets the current estimated queue depth.
    /// Note: This is an estimate due to concurrent processing.
    /// </summary>
    public int CurrentQueueDepth => _queueDepth;
}

/// <summary>
/// Configuration for the atom ingestion channel.
/// </summary>
public sealed class AtomIngestionChannelOptions
{
    /// <summary>
    /// Maximum capacity of the bounded channel.
    /// Default: 1000 requests.
    /// </summary>
    public int Capacity { get; set; } = 1000;

    /// <summary>
    /// Behavior when channel is full.
    /// Default: Wait (backpressure - blocks producers until space available).
    /// </summary>
    public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;

    /// <summary>
    /// Whether to allow single-reader optimization.
    /// Default: true (single consumer worker).
    /// </summary>
    public bool SingleReader { get; set; } = true;

    /// <summary>
    /// Whether to allow single-writer optimization.
    /// Default: false (multiple producers).
    /// </summary>
    public bool SingleWriter { get; set; } = false;
}

/// <summary>
/// Extension methods for registering the atom ingestion worker.
/// </summary>
public static class AtomIngestionWorkerExtensions
{
    /// <summary>
    /// Registers the atom ingestion worker and channel infrastructure.
    /// </summary>
    public static IServiceCollection AddAtomIngestionWorker(
        this IServiceCollection services,
        Action<AtomIngestionChannelOptions>? configureOptions = null)
    {
        var options = new AtomIngestionChannelOptions();
        configureOptions?.Invoke(options);

        // Register the channel as singleton (shared between producer and consumer)
        services.AddSingleton(_ => Channel.CreateBounded<AtomIngestionPipelineRequest>(
            new BoundedChannelOptions(options.Capacity)
            {
                FullMode = options.FullMode,
                SingleReader = options.SingleReader,
                SingleWriter = options.SingleWriter
            }));

        // Register producer (scoped - injected per request)
        services.AddScoped<AtomIngestionProducer>();

        // Register worker (singleton - runs as background service)
        services.AddHostedService<AtomIngestionWorker>();

        // Register pipeline factory (scoped - injected per request)
        services.AddScoped<AtomIngestionPipelineFactory>();

        return services;
    }
}
