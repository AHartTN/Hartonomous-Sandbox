using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Service for extracting student models (compressed/distilled models) from larger parent models.
/// Enables model compression through importance-based pruning, layer extraction, and spatial region sampling.
/// Student models preserve the most important learned features while reducing size for faster inference.
/// </summary>
public interface IStudentModelService
{
    /// <summary>
    /// Extracts a student model by selecting parameters based on importance scores.
    /// Retains the most important weights (highest Z-coordinate values in WeightsGeometry) while pruning others.
    /// </summary>
    /// <param name="parentModelId">ID of the parent model to extract from.</param>
    /// <param name="targetSizeRatio">Target size ratio (0.0 to 1.0) relative to parent model (e.g., 0.5 = 50% of original size).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted student model with reduced parameter count.</returns>
    Task<Model> ExtractByImportanceAsync(int parentModelId, double targetSizeRatio, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a student model by selecting a subset of layers from the parent model.
    /// Creates a shallower model with fewer layers, useful for cascade ensemble systems or early-exit architectures.
    /// </summary>
    /// <param name="parentModelId">ID of the parent model to extract from.</param>
    /// <param name="targetLayerCount">Number of layers to include in the student model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted student model with reduced layer count.</returns>
    Task<Model> ExtractByLayersAsync(int parentModelId, int targetLayerCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts a student model by selecting parameters within a specific spatial region (Z-coordinate range).
    /// Enables targeted extraction of features with particular importance or gradient characteristics.
    /// </summary>
    /// <param name="parentModelId">ID of the parent model to extract from.</param>
    /// <param name="minValue">Minimum Z-coordinate value (importance threshold).</param>
    /// <param name="maxValue">Maximum Z-coordinate value (importance threshold).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted student model containing only parameters in the specified spatial region.</returns>
    Task<Model> ExtractBySpatialRegionAsync(int parentModelId, double minValue, double maxValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two models (typically parent and student) to analyze compression metrics and feature preservation.
    /// </summary>
    /// <param name="modelAId">ID of the first model (typically the parent/larger model).</param>
    /// <param name="modelBId">ID of the second model (typically the student/smaller model).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison metrics including compression ratio, importance scores, and weight overlap.</returns>
    Task<ModelComparisonResult> CompareModelsAsync(int modelAId, int modelBId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of comparing two models, providing metrics on size difference and feature preservation.
/// </summary>
/// <param name="ModelAParameters">Total number of parameters in model A.</param>
/// <param name="ModelBParameters">Total number of parameters in model B.</param>
/// <param name="CompressionRatio">Ratio of model B size to model A size (e.g., 0.5 = model B is 50% the size of model A).</param>
/// <param name="AvgImportanceA">Average importance score (Z-coordinate) of parameters in model A.</param>
/// <param name="AvgImportanceB">Average importance score (Z-coordinate) of parameters in model B.</param>
/// <param name="SharedLayers">Number of layers that exist in both models.</param>
/// <param name="WeightOverlap">Percentage of weights that overlap between the two models (0.0 to 1.0).</param>
public record ModelComparisonResult(
    int ModelAParameters,
    int ModelBParameters,
    double CompressionRatio,
    double AvgImportanceA,
    double AvgImportanceB,
    int SharedLayers,
    double WeightOverlap
);
