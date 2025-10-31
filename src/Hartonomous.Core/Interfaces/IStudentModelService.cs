using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

public interface IStudentModelService
{
    Task<Model> ExtractByImportanceAsync(int parentModelId, double targetSizeRatio, CancellationToken cancellationToken = default);
    Task<Model> ExtractByLayersAsync(int parentModelId, int targetLayerCount, CancellationToken cancellationToken = default);
    Task<Model> ExtractBySpatialRegionAsync(int parentModelId, double minValue, double maxValue, CancellationToken cancellationToken = default);
    Task<ModelComparisonResult> CompareModelsAsync(int modelAId, int modelBId, CancellationToken cancellationToken = default);
}

public record ModelComparisonResult(
    int ModelAParameters,
    int ModelBParameters,
    double CompressionRatio,
    double AvgImportanceA,
    double AvgImportanceB,
    int SharedLayers,
    double WeightOverlap
);
