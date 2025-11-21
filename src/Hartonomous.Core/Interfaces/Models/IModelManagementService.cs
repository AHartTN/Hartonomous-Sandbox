using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Models;

/// <summary>
/// Model management service for weight snapshots and versioning.
/// Provides model checkpoint management and rollback capabilities.
/// </summary>
public interface IModelManagementService
{
    /// <summary>
    /// Create a snapshot of current model weights.
    /// Calls sp_CreateWeightSnapshot stored procedure.
    /// </summary>
    /// <param name="modelId">Model to snapshot</param>
    /// <param name="snapshotName">Descriptive snapshot name</param>
    /// <param name="description">Optional description</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Snapshot ID and metadata</returns>
    Task<WeightSnapshotResult> CreateSnapshotAsync(
        int modelId,
        string snapshotName,
        string? description = null,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List available weight snapshots.
    /// Calls sp_ListWeightSnapshots stored procedure.
    /// </summary>
    /// <param name="modelId">Model to list snapshots for (0 for all)</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available snapshots</returns>
    Task<IEnumerable<SnapshotInfo>> ListSnapshotsAsync(
        int modelId = 0,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore model weights from snapshot.
    /// Calls sp_RestoreWeightSnapshot stored procedure.
    /// </summary>
    /// <param name="snapshotId">Snapshot to restore</param>
    /// <param name="modelId">Target model ID</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RestoreSnapshotAsync(
        int snapshotId,
        int modelId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback weights to specific timestamp.
    /// Calls sp_RollbackWeightsToTimestamp stored procedure.
    /// </summary>
    /// <param name="modelId">Model to rollback</param>
    /// <param name="targetTimestamp">Target timestamp</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackToTimestampAsync(
        int modelId,
        DateTime targetTimestamp,
        int tenantId = 0,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of weight snapshot creation.
/// </summary>
/// <param name="SnapshotId">Created snapshot identifier</param>
/// <param name="SnapshotName">Snapshot name</param>
/// <param name="WeightsCaptured">Number of weights captured</param>
/// <param name="SizeBytes">Snapshot size in bytes</param>
/// <param name="CreatedAt">Creation timestamp</param>
public record WeightSnapshotResult(
    int SnapshotId,
    string SnapshotName,
    int WeightsCaptured,
    long SizeBytes,
    DateTime CreatedAt);

/// <summary>
/// Information about a weight snapshot.
/// </summary>
/// <param name="SnapshotId">Snapshot identifier</param>
/// <param name="ModelId">Source model ID</param>
/// <param name="SnapshotName">Snapshot name</param>
/// <param name="Description">Snapshot description</param>
/// <param name="CreatedAt">When snapshot was created</param>
/// <param name="WeightCount">Number of weights</param>
/// <param name="SizeBytes">Size in bytes</param>
public record SnapshotInfo(
    int SnapshotId,
    int ModelId,
    string SnapshotName,
    string? Description,
    DateTime CreatedAt,
    int WeightCount,
    long SizeBytes);
