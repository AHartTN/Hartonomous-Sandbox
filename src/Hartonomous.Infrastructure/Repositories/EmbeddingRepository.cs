using Hartonomous.Core.Entities;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;

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

    public async Task<IEnumerable<EmbeddingSearchResult>> ExactSearchAsync(
        float[] queryVector, 
        int topK = 10, 
        string metric = "cosine", 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing exact vector search with metric: {Metric}, topK: {TopK}", metric, topK);

        var results = new List<EmbeddingSearchResult>();
        
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
            SELECT TOP (@top_k)
                embedding_id,
                source_text,
                source_type,
                VECTOR_DISTANCE(@distance_metric, embedding_full, @query_vector) as distance,
                1.0 - VECTOR_DISTANCE(@distance_metric, embedding_full, @query_vector) as similarity_score,
                created_at as created_timestamp
            FROM dbo.Embeddings_Production
            WHERE embedding_full IS NOT NULL
            ORDER BY VECTOR_DISTANCE(@distance_metric, embedding_full, @query_vector)";

        var vectorParam = command.CreateParameter();
        vectorParam.ParameterName = "@query_vector";
        vectorParam.Value = SerializeVectorToBytes(queryVector);
        command.Parameters.Add(vectorParam);

        var topKParam = command.CreateParameter();
        topKParam.ParameterName = "@top_k";
        topKParam.Value = topK;
        command.Parameters.Add(topKParam);

        var metricParam = command.CreateParameter();
        metricParam.ParameterName = "@distance_metric";
        metricParam.Value = metric;
        command.Parameters.Add(metricParam);

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new EmbeddingSearchResult
                {
                    EmbeddingId = reader.GetInt64(0),
                    SourceText = reader.GetString(1),
                    SourceType = reader.GetString(2),
                    Distance = reader.GetFloat(3),
                    SimilarityScore = reader.GetFloat(4),
                    CreatedTimestamp = reader.GetDateTime(5)
                });
            }
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

        return results;
    }

    public async Task<IEnumerable<EmbeddingSearchResult>> HybridSearchAsync(
        float[] queryVector, 
        double queryX, 
        double queryY, 
        double queryZ, 
        int spatialCandidates = 100, 
        int finalTopK = 10, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing hybrid search with spatial candidates: {Candidates}, finalTopK: {TopK}", spatialCandidates, finalTopK);

        var results = new List<EmbeddingSearchResult>();
        
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
            DECLARE @query_point GEOMETRY = geometry::STGeomFromText(
                'POINT(' + CAST(@query_spatial_x AS NVARCHAR(50)) + ' ' +
                           CAST(@query_spatial_y AS NVARCHAR(50)) + ' ' +
                           CAST(@query_spatial_z AS NVARCHAR(50)) + ')', 0);

            DECLARE @candidates TABLE (embedding_id BIGINT);

            INSERT INTO @candidates
            SELECT TOP (@spatial_candidates) embedding_id
            FROM dbo.Embeddings_Production WITH(INDEX(idx_spatial_fine))
            WHERE spatial_geometry IS NOT NULL
            ORDER BY spatial_geometry.STDistance(@query_point);

            SELECT TOP (@final_top_k)
                ep.embedding_id,
                ep.source_text,
                ep.source_type,
                VECTOR_DISTANCE('cosine', ep.embedding_full, @query_vector) as distance,
                1.0 - VECTOR_DISTANCE('cosine', ep.embedding_full, @query_vector) as similarity_score,
                ep.created_at as created_timestamp
            FROM dbo.Embeddings_Production ep
            JOIN @candidates c ON ep.embedding_id = c.embedding_id
            ORDER BY VECTOR_DISTANCE('cosine', ep.embedding_full, @query_vector)";

        var vectorParam = command.CreateParameter();
        vectorParam.ParameterName = "@query_vector";
        vectorParam.Value = SerializeVectorToBytes(queryVector);
        command.Parameters.Add(vectorParam);

        var xParam = command.CreateParameter();
        xParam.ParameterName = "@query_spatial_x";
        xParam.Value = queryX;
        command.Parameters.Add(xParam);

        var yParam = command.CreateParameter();
        yParam.ParameterName = "@query_spatial_y";
        yParam.Value = queryY;
        command.Parameters.Add(yParam);

        var zParam = command.CreateParameter();
        zParam.ParameterName = "@query_spatial_z";
        zParam.Value = queryZ;
        command.Parameters.Add(zParam);

        var candidatesParam = command.CreateParameter();
        candidatesParam.ParameterName = "@spatial_candidates";
        candidatesParam.Value = spatialCandidates;
        command.Parameters.Add(candidatesParam);

        var topKParam = command.CreateParameter();
        topKParam.ParameterName = "@final_top_k";
        topKParam.Value = finalTopK;
        command.Parameters.Add(topKParam);

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new EmbeddingSearchResult
                {
                    EmbeddingId = reader.GetInt64(0),
                    SourceText = reader.GetString(1),
                    SourceType = reader.GetString(2),
                    Distance = reader.GetFloat(3),
                    SimilarityScore = reader.GetFloat(4),
                    CreatedTimestamp = reader.GetDateTime(5)
                });
            }
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

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
        var vectorParam = new SqlParameter("@query_vector", new Microsoft.Data.SqlTypes.SqlVector<float>(queryVector));
        var thresholdParam = new SqlParameter("@threshold", threshold);

        var results = await _context.Embeddings
            .FromSqlRaw(
                "EXEC dbo.sp_CheckSimilarityAboveThreshold @query_vector, @threshold",
                vectorParam, thresholdParam)
            .ToListAsync(cancellationToken);  // Execute query first, then get first result

        return results.FirstOrDefault();
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
        command.CommandText = "EXEC dbo.sp_ComputeSpatialProjection @input_vector, @output_x OUTPUT, @output_y OUTPUT, @output_z OUTPUT";
        
        var vectorParam = command.CreateParameter();
        vectorParam.ParameterName = "@input_vector";
        vectorParam.Value = new Microsoft.Data.SqlTypes.SqlVector<float>(fullVector);
        command.Parameters.Add(vectorParam);

        var outputXParam = command.CreateParameter();
        outputXParam.ParameterName = "@output_x";
        outputXParam.Direction = System.Data.ParameterDirection.Output;
        outputXParam.DbType = System.Data.DbType.Double;
        command.Parameters.Add(outputXParam);

        var outputYParam = command.CreateParameter();
        outputYParam.ParameterName = "@output_y";
        outputYParam.Direction = System.Data.ParameterDirection.Output;
        outputYParam.DbType = System.Data.DbType.Double;
        command.Parameters.Add(outputYParam);

        var outputZParam = command.CreateParameter();
        outputZParam.ParameterName = "@output_z";
        outputZParam.Direction = System.Data.ParameterDirection.Output;
        outputZParam.DbType = System.Data.DbType.Double;
        command.Parameters.Add(outputZParam);

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            
            return new float[] 
            { 
                Convert.ToSingle(outputXParam.Value),
                Convert.ToSingle(outputYParam.Value),
                Convert.ToSingle(outputZParam.Value)
            };
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

    public async Task<long> AddWithGeometryAsync(string sourceText, string sourceType, float[] embeddingFull, float[] spatial3D, string contentHash, CancellationToken cancellationToken = default)
    {
        var sql = @"
            INSERT INTO dbo.Embeddings_Production (
                SourceText,
                SourceType,
                embedding_full,
                EmbeddingModel,
                SpatialProjX,
                SpatialProjY,
                SpatialProjZ,
                spatial_geometry,
                spatial_coarse,
                dimension,
                ContentHash,
                AccessCount
            ) VALUES (
                @source_text,
                @source_type,
                @embedding_full,
                @embedding_model,
                @x, @y, @z,
                geometry::STGeomFromText('POINT(' +
                    CAST(@x AS NVARCHAR(50)) + ' ' +
                    CAST(@y AS NVARCHAR(50)) + ')', 0),
                geometry::STGeomFromText('POINT(' +
                    CAST(FLOOR(@x) AS NVARCHAR(50)) + ' ' +
                    CAST(FLOOR(@y) AS NVARCHAR(50)) + ')', 0),
                @dimension,
                @content_hash,
                1
            );
            SELECT SCOPE_IDENTITY();
        ";

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        
        var parameters = new[]
        {
            CreateParameter(command, "@source_text", sourceText),
            CreateParameter(command, "@source_type", sourceType),
            CreateParameter(command, "@embedding_full", new Microsoft.Data.SqlTypes.SqlVector<float>(embeddingFull)),
            CreateParameter(command, "@embedding_model", "production"),
            CreateParameter(command, "@x", spatial3D[0]),
            CreateParameter(command, "@y", spatial3D[1]),
            CreateParameter(command, "@z", spatial3D[2]),
            CreateParameter(command, "@dimension", embeddingFull.Length),
            CreateParameter(command, "@content_hash", contentHash)
        };

        foreach (var param in parameters)
        {
            command.Parameters.Add(param);
        }

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    private DbParameter CreateParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value;
        return param;
    }
}
