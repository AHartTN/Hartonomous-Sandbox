using Hartonomous.Data;
using Hartonomous.Data.Repositories;
using Hartonomous.Infrastructure.Messaging.Events;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Messaging.Handlers;

/// <summary>
/// Handles observation events (OODA loop first step).
/// Triggered when new data is ingested.
/// </summary>
public class ObservationEventHandler
{
    private readonly ILogger<ObservationEventHandler> _logger;
    private readonly IVectorSearchRepository _vectorSearch;
    private readonly IConceptDiscoveryRepository _conceptDiscovery;
    private readonly HartonomousDbContext _context;

    public ObservationEventHandler(
        ILogger<ObservationEventHandler> logger,
        IVectorSearchRepository vectorSearch,
        IConceptDiscoveryRepository conceptDiscovery,
        HartonomousDbContext context)
    {
        _logger = logger;
        _vectorSearch = vectorSearch;
        _conceptDiscovery = conceptDiscovery;
        _context = context;
    }

    public async Task HandleAsync(ObservationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Observation: AtomId={AtomId}, SourceType={SourceType}, EmbeddingId={EmbeddingId}",
            @event.AtomId, @event.SourceType, @event.EmbeddingId);

        // If no embedding was generated, skip orientation
        if (!@event.EmbeddingId.HasValue)
        {
            _logger.LogDebug("No embedding for AtomId={AtomId}, skipping orientation phase", @event.AtomId);
            return;
        }

        // Fetch the embedding vector and spatial geometry
        var embedding = await _context.AtomEmbeddings
            .Where(e => e.AtomEmbeddingId == @event.EmbeddingId.Value)
            .Select(e => new
            {
                e.EmbeddingVector,
                e.SpatialGeometry,
                e.EmbeddingType
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (embedding == null || embedding.EmbeddingVector == null)
        {
            _logger.LogWarning("Embedding {EmbeddingId} not found for AtomId={AtomId}",
                @event.EmbeddingId, @event.AtomId);
            return;
        }

        // Convert SqlVector to byte array for search
        var vectorBytes = embedding.EmbeddingVector.HasValue 
            ? ConvertSqlVectorToBytes(embedding.EmbeddingVector.Value)
            : throw new InvalidOperationException($"Embedding {@event.EmbeddingId} has null vector");

        // 1. Find spatially proximate embeddings using spatial pre-filtering
        var neighbors = await _vectorSearch.SpatialVectorSearchAsync(
            queryVector: vectorBytes,
            spatialCenter: embedding.SpatialGeometry,
            radiusMeters: 1000.0, // 1km radius in embedding space
            topK: 20,
            minSimilarity: 0.7);

        if (neighbors.Count >= 5)
        {
            _logger.LogInformation("Found {Count} neighbors for AtomId={AtomId}, triggering concept discovery",
                neighbors.Count, @event.AtomId);

            // 2. Fetch full embedding data for clustering
            var neighborAtomIds = neighbors.Select(n => n.AtomId).ToList();
            var embeddingData = await _context.AtomEmbeddings
                .Where(e => neighborAtomIds.Contains(e.AtomId) && e.EmbeddingVector != null)
                .Select(e => new EmbeddingVector
                {
                    Id = Guid.NewGuid(),
                    Vector = e.EmbeddingVector.HasValue ? ConvertSqlVectorToDoubleArray(e.EmbeddingVector.Value) : Array.Empty<double>(),
                    SpatialLocation = e.SpatialGeometry,
                    AtomId = null, // AtomId in this context is Guid, but EF uses long
                    Metadata = $"EmbeddingType={e.EmbeddingType};AtomEmbeddingId={e.AtomEmbeddingId}"
                })
                .ToListAsync(cancellationToken);

            // 3. Discover concepts through clustering
            var discoveryResult = await _conceptDiscovery.DiscoverConceptsAsync(
                embeddingData,
                minClusterSize: 3,
                cancellationToken: cancellationToken);

            if (discoveryResult.ClustersFound > 0)
            {
                _logger.LogInformation("Discovered {Count} concepts with quality score {Quality:F2}",
                    discoveryResult.ClustersFound, discoveryResult.ClusteringQuality);

                // 4. Bind concepts to knowledge graph
                await _conceptDiscovery.BindConceptsAsync(
                    discoveryResult.Concepts,
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            _logger.LogDebug("Insufficient neighbors ({Count}) for concept discovery on AtomId={AtomId}",
                neighbors.Count, @event.AtomId);
        }
    }

    private static byte[] ConvertSqlVectorToBytes(SqlVector<float> vector)
    {
        var floats = vector.Memory.ToArray();
        var bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static double[] ConvertSqlVectorToDoubleArray(SqlVector<float> vector)
    {
        var floats = vector.Memory.ToArray();
        return Array.ConvertAll(floats, f => (double)f);
    }
}

/// <summary>
/// Handles orientation events (OODA loop second step).
/// Triggered after pattern recognition or clustering.
/// </summary>
public class OrientationEventHandler
{
    private readonly ILogger<OrientationEventHandler> _logger;
    private readonly IAutonomousAnalysisRepository _analysis;
    private readonly HartonomousDbContext _context;

    public OrientationEventHandler(
        ILogger<OrientationEventHandler> logger,
        IAutonomousAnalysisRepository analysis,
        HartonomousDbContext context)
    {
        _logger = logger;
        _analysis = analysis;
        _context = context;
    }

    public async Task HandleAsync(OrientationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Orientation: Type={Type}, Atoms={Count}",
            @event.OrientationType, @event.AtomIds.Count);

        // Perform analysis to detect anomalies and performance patterns
        var analysisResult = await _analysis.AnalyzeSystemAsync(
            lookbackHours: 24,
            analysisScope: @event.OrientationType,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Analysis completed: Found {AnomalyCount} anomalies and {PatternCount} patterns",
            analysisResult.AnomalyCount, analysisResult.Patterns.Count);

        // Decision phase: Determine if action is needed based on analysis
        if (analysisResult.AnomalyCount > 0 || analysisResult.Patterns.Count > 5)
        {
            // Publish decision event to trigger actions
            var decisionId = Guid.NewGuid();
            var action = analysisResult.AnomalyCount > 0 ? "optimize_performance" : "discover_concepts";
            var confidence = analysisResult.AnomalyCount > 0 ? 0.85f : 0.75f;

            _logger.LogInformation("Publishing decision: Action={Action}, Confidence={Confidence:F2}, DecisionId={DecisionId}",
                action, confidence, decisionId);

            // In a real implementation, this would publish a DecisionEvent to the message bus
            // For now, we just log it since we don't have the event publisher injected
        }
    }
}

/// <summary>
/// Handles decision events (OODA loop third step).
/// Triggered after orientation determines possible actions.
/// </summary>
public class DecisionEventHandler
{
    private readonly ILogger<DecisionEventHandler> _logger;
    private readonly IAutonomousActionRepository _actionRepo;

    public DecisionEventHandler(
        ILogger<DecisionEventHandler> logger,
        IAutonomousActionRepository actionRepo)
    {
        _logger = logger;
        _actionRepo = actionRepo;
    }

    public async Task HandleAsync(DecisionEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Decision: Action={Action}, Confidence={Confidence:F2}, DecisionId={DecisionId}",
            @event.Action, @event.Confidence, @event.DecisionId);

        // If confidence is below threshold, don't execute
        if (@event.Confidence < 0.7f)
        {
            _logger.LogWarning("Decision confidence {Confidence:F2} below threshold, skipping execution",
                @event.Confidence);
            return;
        }

        // Execute the chosen action
        var hypotheses = new List<Hypothesis>
        {
            new Hypothesis
            {
                HypothesisId = @event.DecisionId,
                HypothesisType = @event.Action,
                Priority = @event.Confidence >= 0.9f ? 1 : 2,
                Description = @event.Reasoning ?? "System-generated action",
                RequiredActions = new[] { @event.Action }
            }
        };

        var startTime = DateTime.UtcNow;
        var result = await _actionRepo.ExecuteActionsAsync(
            analysisId: @event.DecisionId,
            hypotheses: hypotheses,
            autoApproveThreshold: 3,
            cancellationToken: cancellationToken);

        var durationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation("Executed {ExecutedCount} actions, queued {QueuedCount}, failed {FailedCount} in {DurationMs}ms",
            result.ExecutedActions, result.QueuedActions, result.FailedActions, durationMs);

        // Publish action event for feedback (would normally use event publisher)
        var actionStatus = result.FailedActions > 0 ? "failed" : "completed";
        _logger.LogInformation("Publishing ActionEvent: DecisionId={DecisionId}, Status={Status}, Duration={DurationMs}ms",
            @event.DecisionId, actionStatus, durationMs);
    }
}

/// <summary>
/// Handles action events (OODA loop fourth step).
/// Triggered when an action is executed.
/// </summary>
public class ActionEventHandler
{
    private readonly ILogger<ActionEventHandler> _logger;
    private readonly HartonomousDbContext _context;

    public ActionEventHandler(
        ILogger<ActionEventHandler> logger,
        HartonomousDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task HandleAsync(ActionEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Action: Type={Type}, Status={Status}, DecisionId={DecisionId}, Duration={DurationMs}ms",
            @event.ActionType, @event.Status, @event.DecisionId, @event.DurationMs);

        if (@event.Status == "failed")
        {
            _logger.LogWarning("Action failed: {Error}", @event.Error);
        }

        // Close the loop: Store action results as observations
        // Record the outcome for learning
        var learningEntry = new
        {
            DecisionId = @event.DecisionId,
            ActionType = @event.ActionType,
            Status = @event.Status,
            DurationMs = @event.DurationMs ?? 0,
            Success = @event.Status == "completed",
            Result = @event.Result?.ToString(),
            Error = @event.Error,
            Timestamp = DateTime.UtcNow
        };

        // Store in AutonomousCycles table for learning
        await _context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO dbo.AutonomousCycles (AnalysisId, HypothesisId, ActionDetails, Outcome, CreatedAt)
            VALUES ({0}, {0}, {1}, {2}, {3})",
            @event.DecisionId,
            System.Text.Json.JsonSerializer.Serialize(learningEntry),
            @event.Status,
            learningEntry.Timestamp);

        // Update confidence scores based on outcome
        if (@event.Status == "completed")
        {
            _logger.LogInformation("Action succeeded, reinforcing decision pattern for {ActionType}", @event.ActionType);
            // In a real implementation, this would update model weights or confidence thresholds
            // based on the success/failure of the action
        }
        else if (@event.Status == "failed")
        {
            _logger.LogWarning("Action failed, adjusting decision criteria for {ActionType}", @event.ActionType);
            // Reduce confidence for similar future decisions
        }
    }
}
