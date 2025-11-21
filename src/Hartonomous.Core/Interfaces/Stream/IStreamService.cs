using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Stream;

/// <summary>
/// Service for stream processing operations.
/// </summary>
public interface IStreamService
{
    /// <summary>
    /// Generates events from stream with clustering.
    /// Calls sp_GenerateEventsFromStream stored procedure.
    /// </summary>
    Task<IEnumerable<StreamEvent>> GenerateEventsAsync(
        int streamId,
        string eventType,
        float threshold = 0.5f,
        string clustering = "dbscan",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fuses multiple modal streams.
    /// Calls sp_FuseMultiModalStreams stored procedure.
    /// </summary>
    Task<FusionResult> FuseStreamsAsync(
        string streamIds,
        string fusionType = "weighted_average",
        string? weights = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Orchestrates sensor event stream with time-bucketed aggregation.
    /// Calls sp_OrchestrateSensorStream stored procedure.
    /// </summary>
    Task<OrchestrationResult> OrchestrateSensorAsync(
        string sensorType,
        DateTime windowStart,
        DateTime windowEnd,
        string aggregationLevel = "minute",
        int maxComponents = 10000,
        CancellationToken cancellationToken = default);
}

public record StreamEvent(
    int EventId,
    string EventType,
    DateTime Timestamp,
    string DataJson,
    int? ClusterId);

public record FusionResult(
    byte[] FusedData,
    int ComponentCount,
    string FusionType);

public record OrchestrationResult(
    int EventsProcessed,
    int BucketsCreated,
    string SummaryJson);
