using Hartonomous.Core.Entities;
using Hartonomous.Core.Models;
using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for atom embedding operations.
/// </summary>
public interface IAtomEmbeddingRepository
{
    Task<AtomEmbedding?> GetByIdAsync(long atomEmbeddingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtomEmbedding>> GetByAtomIdAsync(long atomId, CancellationToken cancellationToken = default);
    Task<AtomEmbedding> AddAsync(AtomEmbedding embedding, CancellationToken cancellationToken = default);
    Task AddComponentsAsync(long atomEmbeddingId, IEnumerable<AtomEmbeddingComponent> components, CancellationToken cancellationToken = default);
    Task<Point> ComputeSpatialProjectionAsync(SqlVector<float> paddedVector, int originalDimension, CancellationToken cancellationToken = default);
    Task<AtomEmbeddingSearchResult?> FindNearestBySimilarityAsync(SqlVector<float> paddedVector, string embeddingType, int? modelId, double maxCosineDistance, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> HybridSearchAsync(float[] vector, Point spatial3D, int spatialCandidates, int finalTopK, CancellationToken cancellationToken = default);
}
