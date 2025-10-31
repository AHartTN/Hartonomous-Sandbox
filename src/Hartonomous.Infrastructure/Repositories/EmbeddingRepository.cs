using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Repositories;

public class EmbeddingRepository : IEmbeddingRepository
{
    private readonly HartonomousDbContext _context;

    public EmbeddingRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<Embedding?> GetByIdAsync(long embeddingId, CancellationToken cancellationToken = default)
    {
        return await _context.Embeddings.FindAsync([embeddingId], cancellationToken);
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
        await _context.Embeddings.AddRangeAsync(embeddings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return embeddings;
    }

    public async Task UpdateAsync(Embedding embedding, CancellationToken cancellationToken = default)
    {
        _context.Embeddings.Update(embedding);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long embeddingId, CancellationToken cancellationToken = default)
    {
        var embedding = await GetByIdAsync(embeddingId, cancellationToken);
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

    public async Task<IEnumerable<EmbeddingSearchResult>> ExactSearchAsync(float[] queryVector, int topK = 10, string metric = "cosine", CancellationToken cancellationToken = default)
    {
        var vectorJson = $"[{string.Join(",", queryVector)}]";
        var sql = $@"
            SELECT TOP ({topK})
                EmbeddingId,
                SourceText,
                SourceType,
                VECTOR_DISTANCE(@metric, EmbeddingFull, CAST(@vectorJson AS VECTOR(768))) as Distance
            FROM dbo.Embeddings
            WHERE EmbeddingFull IS NOT NULL
            ORDER BY VECTOR_DISTANCE(@metric, EmbeddingFull, CAST(@vectorJson AS VECTOR(768)))";

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@metric", metric));
        command.Parameters.Add(new SqlParameter("@vectorJson", vectorJson));

        var results = new List<EmbeddingSearchResult>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var distance = (float)reader.GetDouble(3);
            results.Add(new EmbeddingSearchResult
            {
                EmbeddingId = reader.GetInt64(0),
                SourceText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                SourceType = reader.GetString(2),
                Distance = distance,
                SimilarityScore = 1.0f - distance,
                CreatedTimestamp = DateTime.UtcNow
            });
        }

        return results;
    }

    public async Task<IEnumerable<EmbeddingSearchResult>> HybridSearchAsync(float[] queryVector, double queryX, double queryY, double queryZ, int spatialCandidates = 100, int finalTopK = 10, CancellationToken cancellationToken = default)
    {
        var vectorJson = $"[{string.Join(",", queryVector)}]";
        var sql = $@"
            WITH CoarseResults AS (
                SELECT TOP ({spatialCandidates}) EmbeddingId
                FROM dbo.Embeddings
                WHERE SpatialCoarse IS NOT NULL
                ORDER BY SpatialCoarse.STDistance(geometry::STGeomFromText('POINT(' + CAST(@x AS VARCHAR) + ' ' + CAST(@y AS VARCHAR) + ')', 0))
            )
            SELECT TOP ({finalTopK})
                e.EmbeddingId,
                e.SourceText,
                e.SourceType,
                VECTOR_DISTANCE('cosine', e.EmbeddingFull, CAST(@vectorJson AS VECTOR(768))) as Distance
            FROM dbo.Embeddings e
            INNER JOIN CoarseResults c ON e.EmbeddingId = c.EmbeddingId
            WHERE e.EmbeddingFull IS NOT NULL
            ORDER BY VECTOR_DISTANCE('cosine', e.EmbeddingFull, CAST(@vectorJson AS VECTOR(768)))";

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@x", queryX));
        command.Parameters.Add(new SqlParameter("@y", queryY));
        command.Parameters.Add(new SqlParameter("@vectorJson", vectorJson));

        var results = new List<EmbeddingSearchResult>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var distance = (float)reader.GetDouble(3);
            results.Add(new EmbeddingSearchResult
            {
                EmbeddingId = reader.GetInt64(0),
                SourceText = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                SourceType = reader.GetString(2),
                Distance = distance,
                SimilarityScore = 1.0f - distance,
                CreatedTimestamp = DateTime.UtcNow
            });
        }

        return results;
    }

    public async Task<Embedding?> CheckDuplicateByHashAsync(string contentHash, CancellationToken cancellationToken = default)
    {
        return await _context.Embeddings
            .FirstOrDefaultAsync(e => e.ContentHash == contentHash, cancellationToken);
    }

    public async Task<Embedding?> CheckDuplicateBySimilarityAsync(float[] queryVector, double threshold, CancellationToken cancellationToken = default)
    {
        var vectorJson = $"[{string.Join(",", queryVector)}]";
        var sql = $@"
            SELECT TOP 1 *
            FROM dbo.Embeddings
            WHERE EmbeddingFull IS NOT NULL
              AND VECTOR_DISTANCE('cosine', EmbeddingFull, CAST(@vectorJson AS VECTOR(768))) < @threshold
            ORDER BY VECTOR_DISTANCE('cosine', EmbeddingFull, CAST(@vectorJson AS VECTOR(768)))";

        return await _context.Embeddings
            .FromSqlRaw(sql,
                new SqlParameter("@vectorJson", vectorJson),
                new SqlParameter("@threshold", threshold))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task IncrementAccessCountAsync(long embeddingId, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE dbo.Embeddings SET AccessCount = AccessCount + 1, LastAccessed = SYSUTCDATETIME() WHERE EmbeddingId = {0}",
            embeddingId);
    }

    public async Task<float[]> ComputeSpatialProjectionAsync(float[] fullVector, CancellationToken cancellationToken = default)
    {
        var vectorJson = $"[{string.Join(",", fullVector)}]";
        var sql = @"
            DECLARE @result TABLE (x FLOAT, y FLOAT, z FLOAT);
            EXEC sp_ComputeSpatialProjection @vectorJson;
            SELECT * FROM @result;";

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@vectorJson", vectorJson));

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new[] { (float)reader.GetDouble(0), (float)reader.GetDouble(1), (float)reader.GetDouble(2) };
        }

        return new float[3];
    }

    public async Task<long> AddWithGeometryAsync(string sourceText, string sourceType, float[] embeddingFull, float[] spatial3D, string contentHash, CancellationToken cancellationToken = default)
    {
        var vectorJson = $"[{string.Join(",", embeddingFull)}]";
        var sql = @"
            INSERT INTO dbo.Embeddings (SourceText, SourceType, EmbeddingFull, SpatialProjX, SpatialProjY, SpatialProjZ,
                SpatialGeometry, SpatialCoarse, Dimension, ContentHash, CreatedAt)
            VALUES (@sourceText, @sourceType, CAST(@vectorJson AS VECTOR(768)), @x, @y, @z,
                geometry::STGeomFromText('POINT(' + CAST(@x AS VARCHAR) + ' ' + CAST(@y AS VARCHAR) + ')', 0),
                geometry::STGeomFromText('POINT(' + CAST(FLOOR(@x) AS VARCHAR) + ' ' + CAST(FLOOR(@y) AS VARCHAR) + ')', 0),
                768, @contentHash, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@sourceText", sourceText));
        command.Parameters.Add(new SqlParameter("@sourceType", sourceType));
        command.Parameters.Add(new SqlParameter("@vectorJson", vectorJson));
        command.Parameters.Add(new SqlParameter("@x", spatial3D[0]));
        command.Parameters.Add(new SqlParameter("@y", spatial3D[1]));
        command.Parameters.Add(new SqlParameter("@z", spatial3D[2]));
        command.Parameters.Add(new SqlParameter("@contentHash", contentHash));

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }
}
