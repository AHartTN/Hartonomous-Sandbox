using System.Diagnostics.Metrics;

namespace Hartonomous.Infrastructure.Observability;

/// <summary>
/// Custom OpenTelemetry metrics for Hartonomous system monitoring
/// </summary>
public class CustomMetrics
{
    private readonly Meter _meter;

    // Cache metrics
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Histogram<double> _cacheOperationDuration;

    // Job processing metrics
    private readonly Counter<long> _jobsProcessed;
    private readonly Counter<long> _jobsFailed;
    private readonly Histogram<double> _jobProcessingDuration;
    private readonly UpDownCounter<long> _activeJobs;

    // OODA loop metrics
    private readonly Counter<long> _oodaCyclesCompleted;
    private readonly Histogram<double> _oodaCycleDuration;
    private readonly Counter<long> _observationsProcessed;
    private readonly Counter<long> _decisionsExecuted;

    // Event bus metrics
    private readonly Counter<long> _eventsPublished;
    private readonly Counter<long> _eventsFailed;
    private readonly Histogram<double> _eventPublishDuration;
    private readonly UpDownCounter<long> _eventBacklogSize;

    // Inference metrics
    private readonly Counter<long> _inferenceRequests;
    private readonly Counter<long> _inferenceErrors;
    private readonly Histogram<double> _inferenceDuration;
    private readonly Histogram<long> _inferenceTokens;

    // Embedding metrics
    private readonly Counter<long> _embeddingsGenerated;
    private readonly Histogram<double> _embeddingGenerationDuration;
    private readonly Histogram<long> _embeddingDimensions;

    // Graph metrics
    private readonly Counter<long> _atomsIngested;
    private readonly Counter<long> _graphUpdates;
    private readonly Histogram<double> _graphTraversalDuration;

    public CustomMetrics(Meter meter)
    {
        _meter = meter;

        // Cache
        _cacheHits = _meter.CreateCounter<long>("hartonomous.cache.hits", "hits", "Number of cache hits");
        _cacheMisses = _meter.CreateCounter<long>("hartonomous.cache.misses", "misses", "Number of cache misses");
        _cacheOperationDuration = _meter.CreateHistogram<double>("hartonomous.cache.operation.duration", "ms", "Cache operation duration");

        // Jobs
        _jobsProcessed = _meter.CreateCounter<long>("hartonomous.jobs.processed", "jobs", "Number of background jobs processed");
        _jobsFailed = _meter.CreateCounter<long>("hartonomous.jobs.failed", "jobs", "Number of background jobs that failed");
        _jobProcessingDuration = _meter.CreateHistogram<double>("hartonomous.jobs.duration", "ms", "Job processing duration");
        _activeJobs = _meter.CreateUpDownCounter<long>("hartonomous.jobs.active", "jobs", "Number of currently active jobs");

        // OODA
        _oodaCyclesCompleted = _meter.CreateCounter<long>("hartonomous.ooda.cycles.completed", "cycles", "Number of OODA cycles completed");
        _oodaCycleDuration = _meter.CreateHistogram<double>("hartonomous.ooda.cycle.duration", "ms", "OODA cycle duration (Observe→Orient→Decide→Act)");
        _observationsProcessed = _meter.CreateCounter<long>("hartonomous.ooda.observations", "observations", "Number of observations processed");
        _decisionsExecuted = _meter.CreateCounter<long>("hartonomous.ooda.decisions", "decisions", "Number of decisions executed");

        // Events
        _eventsPublished = _meter.CreateCounter<long>("hartonomous.events.published", "events", "Number of events published to event bus");
        _eventsFailed = _meter.CreateCounter<long>("hartonomous.events.failed", "events", "Number of events that failed to publish");
        _eventPublishDuration = _meter.CreateHistogram<double>("hartonomous.events.publish.duration", "ms", "Event publish duration");
        _eventBacklogSize = _meter.CreateUpDownCounter<long>("hartonomous.events.backlog", "events", "Number of events waiting in backlog");

        // Inference
        _inferenceRequests = _meter.CreateCounter<long>("hartonomous.inference.requests", "requests", "Number of inference requests");
        _inferenceErrors = _meter.CreateCounter<long>("hartonomous.inference.errors", "errors", "Number of inference errors");
        _inferenceDuration = _meter.CreateHistogram<double>("hartonomous.inference.duration", "ms", "Inference duration");
        _inferenceTokens = _meter.CreateHistogram<long>("hartonomous.inference.tokens", "tokens", "Number of tokens processed in inference");

        // Embeddings
        _embeddingsGenerated = _meter.CreateCounter<long>("hartonomous.embeddings.generated", "embeddings", "Number of embeddings generated");
        _embeddingGenerationDuration = _meter.CreateHistogram<double>("hartonomous.embeddings.duration", "ms", "Embedding generation duration");
        _embeddingDimensions = _meter.CreateHistogram<long>("hartonomous.embeddings.dimensions", "dimensions", "Embedding vector dimensions");

        // Graph
        _atomsIngested = _meter.CreateCounter<long>("hartonomous.graph.atoms.ingested", "atoms", "Number of atoms ingested into graph");
        _graphUpdates = _meter.CreateCounter<long>("hartonomous.graph.updates", "updates", "Number of graph updates");
        _graphTraversalDuration = _meter.CreateHistogram<double>("hartonomous.graph.traversal.duration", "ms", "Graph traversal duration");
    }

    // Cache methods
    public void RecordCacheHit(string cacheType) => _cacheHits.Add(1, [new("cache_type", cacheType)]);
    public void RecordCacheMiss(string cacheType) => _cacheMisses.Add(1, [new("cache_type", cacheType)]);
    public void RecordCacheOperation(string operation, double durationMs) => _cacheOperationDuration.Record(durationMs, [new("operation", operation)]);

    // Job methods
    public void RecordJobProcessed(string jobType) => _jobsProcessed.Add(1, [new("job_type", jobType)]);
    public void RecordJobFailed(string jobType, string error) => _jobsFailed.Add(1, [new("job_type", jobType), new("error", error)]);
    public void RecordJobDuration(string jobType, double durationMs) => _jobProcessingDuration.Record(durationMs, [new("job_type", jobType)]);
    public void IncrementActiveJobs(string jobType) => _activeJobs.Add(1, [new("job_type", jobType)]);
    public void DecrementActiveJobs(string jobType) => _activeJobs.Add(-1, [new("job_type", jobType)]);

    // OODA methods
    public void RecordOodaCycleCompleted(double durationMs) => _oodaCyclesCompleted.Add(1);
    public void RecordOodaCycleDuration(double durationMs) => _oodaCycleDuration.Record(durationMs);
    public void RecordObservation(string sourceType) => _observationsProcessed.Add(1, [new("source_type", sourceType)]);
    public void RecordDecision(string actionType, double confidence) => _decisionsExecuted.Add(1, [new("action", actionType), new("confidence", confidence.ToString("F2"))]);

    // Event methods
    public void RecordEventPublished(string eventType) => _eventsPublished.Add(1, [new("event_type", eventType)]);
    public void RecordEventFailed(string eventType, string error) => _eventsFailed.Add(1, [new("event_type", eventType), new("error", error)]);
    public void RecordEventPublishDuration(string eventType, double durationMs) => _eventPublishDuration.Record(durationMs, [new("event_type", eventType)]);
    public void IncrementEventBacklog(string topic) => _eventBacklogSize.Add(1, [new("topic", topic)]);
    public void DecrementEventBacklog(string topic) => _eventBacklogSize.Add(-1, [new("topic", topic)]);

    // Inference methods
    public void RecordInferenceRequest(string modelId) => _inferenceRequests.Add(1, [new("model_id", modelId)]);
    public void RecordInferenceError(string modelId, string error) => _inferenceErrors.Add(1, [new("model_id", modelId), new("error", error)]);
    public void RecordInferenceDuration(string modelId, double durationMs) => _inferenceDuration.Record(durationMs, [new("model_id", modelId)]);
    public void RecordInferenceTokens(string modelId, long tokens) => _inferenceTokens.Record(tokens, [new("model_id", modelId)]);

    // Embedding methods
    public void RecordEmbeddingGenerated(string sourceType) => _embeddingsGenerated.Add(1, [new("source_type", sourceType)]);
    public void RecordEmbeddingDuration(string sourceType, double durationMs) => _embeddingGenerationDuration.Record(durationMs, [new("source_type", sourceType)]);
    public void RecordEmbeddingDimensions(int dimensions) => _embeddingDimensions.Record(dimensions);

    // Graph methods
    public void RecordAtomIngested(string contentType) => _atomsIngested.Add(1, [new("content_type", contentType)]);
    public void RecordGraphUpdate(string updateType) => _graphUpdates.Add(1, [new("update_type", updateType)]);
    public void RecordGraphTraversal(double durationMs, int nodeCount) => _graphTraversalDuration.Record(durationMs, [new("node_count", nodeCount.ToString())]);
}
