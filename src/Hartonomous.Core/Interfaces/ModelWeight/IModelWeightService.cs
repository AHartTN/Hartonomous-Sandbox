using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.ModelWeight;

/// <summary>
/// Service for model weight management operations.
/// </summary>
public interface IModelWeightService
{
    /// <summary>
    /// Creates checkpoint of model weights.
    /// Calls sp_CreateWeightSnapshot stored procedure.
    /// </summary>
    Task<int> CreateSnapshotAsync(
        int modelId,
        string snapshotName,
        string? snapshotDescription = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores model weights from a snapshot.
    /// Calls sp_RestoreWeightSnapshot stored procedure.
    /// </summary>
    Task RestoreSnapshotAsync(
        int snapshotId,
        int modelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists weight snapshots for a model.
    /// Calls sp_ListWeightSnapshots stored procedure.
    /// </summary>
    Task<IEnumerable<WeightSnapshot>> ListSnapshotsAsync(
        int modelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries model weights for layer or full model.
    /// Calls sp_QueryModelWeights stored procedure.
    /// </summary>
    Task<IEnumerable<ModelWeight>> QueryWeightsAsync(
        int modelId,
        string? layerName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back model weights to specific timestamp.
    /// Calls sp_RollbackWeightsToTimestamp stored procedure.
    /// </summary>
    Task RollbackAsync(
        int modelId,
        DateTime timestamp,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconstructs model weights from storage/snapshot.
    /// Calls sp_ReconstructModelWeights stored procedure.
    /// </summary>
    Task<IEnumerable<ReconstructedWeight>> ReconstructAsync(
        int modelId,
        int? snapshotId = null,
        CancellationToken cancellationToken = default);
}

public record WeightSnapshot(
    int SnapshotId,
    int ModelId,
    string Name,
    string? Description,
    DateTime CreatedAt);

public record ModelWeight(
    int LayerIndex,
    string LayerName,
    long WeightIndex,
    float Value);

public record ReconstructedWeight(
    string TensorName,
    int LayerIndex,
    byte[] WeightData,
    string Shape);
