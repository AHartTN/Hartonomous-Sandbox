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

    // Deduplication methods (Phase 2)
    public async Task<Embedding?> CheckDuplicateByHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        return await _context.Embeddings
            .Where(e => e.ContentHash == contentHash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Embedding?> CheckDuplicateBySimilarityAsync(float[] queryVector, double threshold, CancellationToken cancellationToken = default)
    {
        // Use stored proc for VECTOR_DISTANCE calculation
        // Note: This will require creating sp_CheckSimilarityAboveThreshold stored proc
        var vectorParam = new SqlParameter("@query_vector", System.Data.SqlDbType.VarBinary)
        {
            Value = SerializeVectorToBytes(queryVector)
        };
        var thresholdParam = new SqlParameter("@threshold", threshold);

        var results = await _context.Embeddings
            .FromSqlRaw(
                "EXEC dbo.sp_CheckSimilarityAboveThreshold @query_vector, @threshold",
                vectorParam, thresholdParam)
            .FirstOrDefaultAsync(cancellationToken);

        return results;
    }

    public async Task IncrementAccessCountAsync(long embeddingId, CancellationToken cancellationToken = default)
    {
        var embedding = await _context.Embeddings.FindAsync(new object[] { embeddingId }, cancellationToken);
        if (embedding != null)
        {
            embedding.AccessCount++;
            embedding.LastAccessed = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<float[]> ComputeSpatialProjectionAsync(float[] fullVector, CancellationToken cancellationToken = default)
    {
        // Call stored proc sp_ComputeSpatialProjection
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "EXEC dbo.sp_ComputeSpatialProjection @input_vector";
        
        var vectorParam = command.CreateParameter();
        vectorParam.ParameterName = "@input_vector";
        vectorParam.Value = SerializeVectorToBytes(fullVector);
        command.Parameters.Add(vectorParam);

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            using var result = await command.ExecuteReaderAsync(cancellationToken);
            if (await result.ReadAsync(cancellationToken))
            {
                return new float[] 
                { 
                    result.GetFloat(0), 
                    result.GetFloat(1), 
                    result.GetFloat(2) 
                };
            }
            
            throw new InvalidOperationException("Failed to compute spatial projection");
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    // Helper method to serialize float array to bytes for VECTOR parameter
    private byte[] SerializeVectorToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
