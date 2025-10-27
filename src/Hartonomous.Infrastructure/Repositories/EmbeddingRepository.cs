using Hartonomous.Core.Entities;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

public class EmbeddingRepository : IEmbeddingRepository
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<EmbeddingRepository> _logger;

    public EmbeddingRepository(HartonomousDbContext context, ILogger<EmbeddingRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Embedding?> GetByIdAsync(long embeddingId, CancellationToken cancellationToken = default)
    {
        return await _context.Embeddings.FindAsync(new object[] { embeddingId }, cancellationToken);
    }

    public async Task<IEnumerable<Embedding>> GetBySourceTypeAsync(string sourceType, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Embeddings
            .Where(e => e.SourceType == sourceType)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Embedding> AddAsync(Embedding embedding, CancellationToken cancellationToken = default)
    {
        _context.Embeddings.Add(embedding);
        await _context.SaveChangesAsync(cancellationToken);
        return embedding;
    }

    public async Task<IEnumerable<Embedding>> AddRangeAsync(IEnumerable<Embedding> embeddings, CancellationToken cancellationToken = default)
    {
        var embeddingList = embeddings.ToList();
        _logger.LogInformation("Adding {Count} embeddings in batch", embeddingList.Count);
        
        _context.Embeddings.AddRange(embeddingList);
        await _context.SaveChangesAsync(cancellationToken);
        
        return embeddingList;
    }

    public async Task UpdateAsync(Embedding embedding, CancellationToken cancellationToken = default)
    {
        _context.Embeddings.Update(embedding);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long embeddingId, CancellationToken cancellationToken = default)
    {
        var embedding = await _context.Embeddings.FindAsync(new object[] { embeddingId }, cancellationToken);
        if (embedding != null)
        {
            _context.Embeddings.Remove(embedding);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Embeddings.CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<Embedding>> ExactSearchAsync(
        string queryVector, 
        int topK = 10, 
        string metric = "cosine", 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing exact vector search with metric: {Metric}, topK: {TopK}", metric, topK);

        // Call stored procedure sp_ExactVectorSearch
        var results = await _context.Embeddings
            .FromSqlRaw(
                "EXEC dbo.sp_ExactVectorSearch @query_vector = {0}, @top_k = {1}, @distance_metric = {2}",
                queryVector, topK, metric)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<IEnumerable<Embedding>> HybridSearchAsync(
        string queryVector, 
        double queryX, 
        double queryY, 
        double queryZ, 
        int spatialCandidates = 100, 
        int finalTopK = 10, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing hybrid search with spatial candidates: {Candidates}, finalTopK: {TopK}", spatialCandidates, finalTopK);

        // Call stored procedure sp_HybridSearch
        var results = await _context.Embeddings
            .FromSqlRaw(
                @"EXEC dbo.sp_HybridSearch 
                    @query_vector = {0}, 
                    @query_spatial_x = {1}, 
                    @query_spatial_y = {2}, 
                    @query_spatial_z = {3},
                    @spatial_candidates = {4},
                    @final_top_k = {5}",
                queryVector, queryX, queryY, queryZ, spatialCandidates, finalTopK)
            .ToListAsync(cancellationToken);

        return results;
    }
}
