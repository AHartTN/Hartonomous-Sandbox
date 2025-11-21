using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Streaming;

/// <summary>
/// Stream processing service for real-time event orchestration.
/// Handles sensor streams, multi-modal fusion, and event generation.
/// </summary>
public interface IStreamProcessingService
{
    /// <summary>
    /// Orchestrate sensor stream processing.
    /// Calls sp_OrchestrateSensorStream stored procedure.
    /// </summary>
    /// <param name="streamId">Stream identifier</param>
    /// <param name="sensorType">Type of sensor (camera, lidar, audio, etc.)</param>
    /// <param name="windowSizeMs">Processing window size in milliseconds</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream orchestration result</returns>
    Task<StreamOrchestrationResult> OrchestrateAsync(
        Guid streamId,
        string sensorType,
        int windowSizeMs = 1000,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fuse multiple modal streams into unified representation.
    /// Calls sp_FuseMultiModalStreams stored procedure.
    /// </summary>
    /// <param name="streamIds">Comma-separated stream IDs to fuse</param>
    /// <param name="fusionStrategy">Fusion strategy (concat, attention, ensemble)</param>
    /// <param name="synchronizationToleranceMs">Time sync tolerance</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fused stream result</returns>
    Task<MultiModalFusionResult> FuseStreamsAsync(
        string streamIds,
        string fusionStrategy = "attention",
        int synchronizationToleranceMs = 100,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate discrete events from continuous stream.
    /// Calls sp_GenerateEventsFromStream stored procedure.
    /// </summary>
    /// <param name="streamId">Source stream identifier</param>
    /// <param name="eventThreshold">Event detection threshold</param>
    /// <param name="minEventDurationMs">Minimum event duration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated events</returns>
    Task<int> GenerateEventsAsync(
        Guid streamId,
        float eventThreshold = 0.7f,
        int minEventDurationMs = 500,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of stream orchestration.
/// </summary>
/// <param name="StreamId">Orchestrated stream ID</param>
/// <param name="EventsProcessed">Number of events processed</param>
/// <param name="LatencyMs">Processing latency</param>
/// <param name="Throughput">Events per second</param>
public record StreamOrchestrationResult(
    Guid StreamId,
    int EventsProcessed,
    int LatencyMs,
    float Throughput);

/// <summary>
/// Result of multi-modal stream fusion.
/// </summary>
/// <param name="FusedStreamId">ID of fused stream</param>
/// <param name="ModalitiesFused">Number of modalities combined</param>
/// <param name="SynchronizationQuality">Sync quality score (0.0-1.0)</param>
public record MultiModalFusionResult(
    Guid FusedStreamId,
    int ModalitiesFused,
    float SynchronizationQuality);
