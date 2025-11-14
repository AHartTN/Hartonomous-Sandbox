using Hartonomous.Data.Entities;

namespace Hartonomous.Core.Interfaces;

public interface ILayerTensorSegmentRepository
{
    Task<LayerTensorSegment?> GetByIdAsync(long segmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LayerTensorSegment>> GetByLayerAsync(long layerId, CancellationToken cancellationToken = default);
    Task<LayerTensorSegment> AddAsync(LayerTensorSegment segment, CancellationToken cancellationToken = default);
    Task BulkInsertAsync(IEnumerable<LayerTensorSegment> segments, CancellationToken cancellationToken = default);
    Task DeleteByLayerAsync(long layerId, CancellationToken cancellationToken = default);
}