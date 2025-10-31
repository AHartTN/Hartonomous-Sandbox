using Hartonomous.Core.Entities;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Interfaces;

public interface IModelLayerRepository
{
    Task<ModelLayer?> GetByIdAsync(long layerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelLayer>> GetByModelAsync(int modelId, CancellationToken cancellationToken = default);
    Task<ModelLayer> AddAsync(ModelLayer layer, CancellationToken cancellationToken = default);
    Task BulkInsertAsync(IEnumerable<ModelLayer> layers, CancellationToken cancellationToken = default);
    Task UpdateAsync(ModelLayer layer, CancellationToken cancellationToken = default);
    Task DeleteAsync(long layerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelLayer>> GetLayersByWeightRangeAsync(int modelId, double minValue, double maxValue, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelLayer>> GetLayersByImportanceAsync(int modelId, double minImportance, CancellationToken cancellationToken = default);
    float[] ExtractWeightsFromGeometry(LineString geometry);
    LineString CreateGeometryFromWeights(float[] weights, float[]? importanceScores = null, float[]? temporalMetadata = null);
}
