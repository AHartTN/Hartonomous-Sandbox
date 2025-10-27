using Hartonomous.Core.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Embedding entity operations
/// </summary>
public interface IEmbeddingRepository
{
    Task<Embedding?> GetByIdAsync(long embeddingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Embedding>> GetBySourceTypeAsync(string sourceType, int limit = 100, CancellationToken cancellationToken = default);
    Task<Embedding> AddAsync(Embedding embedding, CancellationToken cancellationToken = default);
    Task<IEnumerable<Embedding>> AddRangeAsync(IEnumerable<Embedding> embeddings, CancellationToken cancellationToken = default);
    Task UpdateAsync(Embedding embedding, CancellationToken cancellationToken = default);
    Task DeleteAsync(long embeddingId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    
    // Vector search methods (will call stored procedures)
    Task<IEnumerable<Embedding>> ExactSearchAsync(string queryVector, int topK = 10, string metric = "cosine", CancellationToken cancellationToken = default);
    Task<IEnumerable<Embedding>> HybridSearchAsync(string queryVector, double queryX, double queryY, double queryZ, int spatialCandidates = 100, int finalTopK = 10, CancellationToken cancellationToken = default);
    
    // Deduplication methods (Phase 2 addition)
    Task<Embedding?> CheckDuplicateByHashAsync(string contentHash, CancellationToken cancellationToken = default);
    Task<Embedding?> CheckDuplicateBySimilarityAsync(float[] queryVector, double threshold, CancellationToken cancellationToken = default);
    Task IncrementAccessCountAsync(long embeddingId, CancellationToken cancellationToken = default);
    
    // Spatial projection method (Phase 2 addition)
    Task<float[]> ComputeSpatialProjectionAsync(float[] fullVector, CancellationToken cancellationToken = default);
}
