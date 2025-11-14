using Hartonomous.Infrastructure.Messaging.Events;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Observability;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Hartonomous.Infrastructure.Messaging.Handlers;

/// <summary>
/// Handles OODA Loop Observation events - triggered when new atoms are ingested.
/// Performs initial vector search and spatial clustering.
/// </summary>
public class ObservationEventHandler
{
    private readonly IAtomEmbeddingRepository _embeddingRepository;
    private readonly IAtomRepository _atomRepository;
    private readonly IEventBus _eventBus;
    private readonly CustomMetrics _metrics;
    private readonly ILogger<ObservationEventHandler> _logger;

    public ObservationEventHandler(
        IAtomEmbeddingRepository embeddingRepository,
        IAtomRepository atomRepository,
        IEventBus eventBus,
        CustomMetrics metrics,
        ILogger<ObservationEventHandler> logger)
    {
        _embeddingRepository = embeddingRepository;
        _atomRepository = atomRepository;
        _eventBus = eventBus;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task HandleAsync(ObservationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OODA Observation: AtomId={AtomId}, SourceType={SourceType}",
            @event.AtomId, @event.SourceType);

        _metrics.RecordObservation(@event.SourceType);

        try
        {
            // If embedding was generated, find similar atoms using hybrid search
            if (@event.EmbeddingId.HasValue)
            {
                var embedding = await _embeddingRepository.GetByIdAsync(@event.EmbeddingId.Value, cancellationToken);
                if (embedding?.EmbeddingVector != null && embedding.SpatialProjection3D != null)
                {
                    // Use hybrid search to find similar embeddings
                    var vectorSpan = embedding.EmbeddingVector.AsReadOnlySpan();
                    var vectorArray = vectorSpan.ToArray();
                    var similarEmbeddings = await _embeddingRepository.HybridSearchAsync(
                        vectorArray,
                        (Point)embedding.SpatialProjection3D,
                        spatialCandidates: 50,
                        finalTopK: 10,
                        cancellationToken);

                    if (similarEmbeddings.Any())
                    {
                        // Publish orientation event for pattern analysis
                        var orientationEvent = new OrientationEvent
                        {
                            AtomIds = similarEmbeddings.Select(s => s.Embedding.AtomId).Prepend(@event.AtomId).ToList(),
                            OrientationType = "clustering",
                            Similarities = similarEmbeddings.ToDictionary(
                                s => s.Embedding.AtomId.ToString(),
                                s => (float)s.CosineDistance),
                            TenantId = @event.TenantId,
                            UserId = @event.UserId,
                            CorrelationId = @event.CorrelationId
                        };

                        await _eventBus.PublishAsync(orientationEvent, cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    _logger.LogDebug("Embedding {EmbeddingId} has no vector or spatial projection, skipping similarity search",
                        @event.EmbeddingId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling observation event for AtomId={AtomId}", @event.AtomId);
            throw;
        }
    }
}

/// <summary>
/// Handles OODA Loop Orientation events - analyzes patterns and correlations.
/// Determines what actions might be beneficial based on observed patterns.
/// </summary>
public class OrientationEventHandler
{
    private readonly IAtomRepository _atomRepository;
    private readonly IInferenceRepository _inferenceRepository;
    private readonly IEventBus _eventBus;
    private readonly CustomMetrics _metrics;
    private readonly ILogger<OrientationEventHandler> _logger;

    public OrientationEventHandler(
        IAtomRepository atomRepository,
        IInferenceRepository inferenceRepository,
        IEventBus eventBus,
        CustomMetrics metrics,
        ILogger<OrientationEventHandler> logger)
    {
        _atomRepository = atomRepository;
        _inferenceRepository = inferenceRepository;
        _eventBus = eventBus;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task HandleAsync(OrientationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OODA Orientation: Type={Type}, AtomCount={Count}",
            @event.OrientationType, @event.AtomIds.Count);

        try
        {
            // Get atoms from the cluster
            var atomTasks = @event.AtomIds.Select(id => _atomRepository.GetByIdAsync(id, cancellationToken));
            var atoms = (await Task.WhenAll(atomTasks)).Where(a => a != null).ToList();
            
            if (atoms.Count < 2)
            {
                _logger.LogDebug("Insufficient atoms ({Count}) for orientation analysis", atoms.Count);
                return;
            }

            // Determine optimal action based on pattern type
            string action;
            float confidence;
            string reasoning;

            switch (@event.OrientationType)
            {
                case "clustering":
                    // Similar content detected - consider caching or indexing
                    action = "cache_warming";
                    confidence = 0.85f;
                    reasoning = $"Detected cluster of {atoms.Count} similar atoms - proactive caching recommended";
                    break;

                case "correlation":
                    // Correlated patterns - consider inference optimization
                    action = "inference_optimization";
                    confidence = 0.75f;
                    reasoning = $"Found {atoms.Count} correlated atoms - inference path optimization recommended";
                    break;

                case "anomaly_detection":
                    // Unusual pattern - consider alerting or special handling
                    action = "anomaly_alert";
                    confidence = 0.90f;
                    reasoning = $"Anomaly detected across {atoms.Count} atoms - investigation recommended";
                    break;

                default:
                    _logger.LogWarning("Unknown orientation type: {OrientationType}", @event.OrientationType);
                    return;
            }

            // Publish decision event
            var decisionEvent = new DecisionEvent
            {
                DecisionId = Guid.NewGuid(),
                Action = action,
                Confidence = confidence,
                DecisionMaker = "OrientationEventHandler",
                Reasoning = reasoning,
                TenantId = @event.TenantId,
                UserId = @event.UserId,
                CorrelationId = @event.CorrelationId
            };

            await _eventBus.PublishAsync(decisionEvent, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling orientation event, Type={Type}", @event.OrientationType);
            throw;
        }
    }
}

/// <summary>
/// Handles OODA Loop Decision events - validates and prioritizes actions.
/// Ensures decisions meet safety constraints before execution.
/// </summary>
public class DecisionEventHandler
{
    private readonly IEventBus _eventBus;
    private readonly CustomMetrics _metrics;
    private readonly ILogger<DecisionEventHandler> _logger;

    public DecisionEventHandler(
        IEventBus eventBus,
        CustomMetrics metrics,
        ILogger<DecisionEventHandler> logger)
    {
        _eventBus = eventBus;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task HandleAsync(DecisionEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OODA Decision: Action={Action}, Confidence={Confidence}",
            @event.Action, @event.Confidence);

        _metrics.RecordDecision(@event.Action, @event.Confidence);

        try
        {
            // Validate decision meets safety constraints
            if (@event.Confidence < 0.6f)
            {
                _logger.LogWarning("Decision confidence too low ({Confidence}), skipping action {Action}",
                    @event.Confidence, @event.Action);
                return;
            }

            // Publish action event to execute the decision
            var actionEvent = new ActionEvent
            {
                DecisionId = @event.DecisionId,
                ActionType = @event.Action,
                Status = "initiated",
                TenantId = @event.TenantId,
                UserId = @event.UserId,
                CorrelationId = @event.CorrelationId
            };

            await _eventBus.PublishAsync(actionEvent, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling decision event, DecisionId={DecisionId}", @event.DecisionId);
            throw;
        }
    }
}

/// <summary>
/// Handles OODA Loop Action events - executes autonomous improvements.
/// Performs the actual work decided by the OODA loop (caching, indexing, etc.).
/// </summary>
public class ActionEventHandler
{
    private readonly ILogger<ActionEventHandler> _logger;
    private readonly CustomMetrics _metrics;

    public ActionEventHandler(
        ILogger<ActionEventHandler> logger,
        CustomMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public async Task HandleAsync(ActionEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OODA Action: Type={Type}, Status={Status}, DecisionId={DecisionId}",
            @event.ActionType, @event.Status, @event.DecisionId);

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Execute action based on type
            switch (@event.ActionType)
            {
                case "cache_warming":
                    _logger.LogInformation("Executing cache warming action");
                    // TODO: Trigger cache warming job
                    break;

                case "inference_optimization":
                    _logger.LogInformation("Executing inference optimization");
                    // TODO: Trigger inference path optimization
                    break;

                case "anomaly_alert":
                    _logger.LogWarning("Anomaly detected - alerting administrators");
                    // TODO: Send alert notification
                    break;

                default:
                    _logger.LogWarning("Unknown action type: {ActionType}", @event.ActionType);
                    break;
            }

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("OODA Action completed: {ActionType} in {Duration}ms",
                @event.ActionType, duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action {ActionType}", @event.ActionType);
            throw;
        }

        await Task.CompletedTask;
    }
}
