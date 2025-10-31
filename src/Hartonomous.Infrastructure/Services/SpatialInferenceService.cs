using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Services;

public class SpatialInferenceService : ISpatialInferenceService
{
    private readonly HartonomousDbContext _context;
    private readonly IEmbeddingRepository _embeddingRepository;

    public SpatialInferenceService(HartonomousDbContext context, IEmbeddingRepository embeddingRepository)
    {
        _context = context;
        _embeddingRepository = embeddingRepository;
    }

    public async Task<IReadOnlyList<(long TokenId, string Token, double AttentionWeight)>> SpatialAttentionAsync(
        long queryTokenId,
        int contextSize,
        CancellationToken cancellationToken = default)
    {
        var sql = $@"
            DECLARE @querySpatial GEOMETRY;
            SELECT @querySpatial = SpatialGeometry
            FROM dbo.Embeddings
            WHERE EmbeddingId = @queryTokenId;

            SELECT TOP ({contextSize})
                e.EmbeddingId as TokenId,
                e.SourceText as Token,
                1.0 / (1.0 + e.SpatialGeometry.STDistance(@querySpatial)) as AttentionWeight
            FROM dbo.Embeddings e
            WHERE e.SpatialGeometry IS NOT NULL
              AND e.EmbeddingId != @queryTokenId
            ORDER BY e.SpatialGeometry.STDistance(@querySpatial)";

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@queryTokenId", queryTokenId));

        var results = new List<(long, string, double)>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add((
                reader.GetInt64(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.GetDouble(2)
            ));
        }

        return results;
    }

    public async Task<IReadOnlyList<(long TokenId, string Token, double Probability)>> PredictNextTokenAsync(
        IEnumerable<long> contextTokenIds,
        double temperature,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var tokenIdsStr = string.Join(",", contextTokenIds);

        var sql = $@"
            DECLARE @contextCentroid GEOMETRY;
            SELECT @contextCentroid = geometry::STGeomFromText(
                'POINT(' +
                CAST(AVG(SpatialGeometry.STX) AS NVARCHAR(50)) + ' ' +
                CAST(AVG(SpatialGeometry.STY) AS NVARCHAR(50)) + ')',
                0
            )
            FROM dbo.Embeddings
            WHERE EmbeddingId IN (SELECT value FROM STRING_SPLIT(@tokenIds, ','));

            SELECT TOP ({topK})
                EmbeddingId as TokenId,
                SourceText as Token,
                EXP(-1 * SpatialGeometry.STDistance(@contextCentroid) / @temperature) as Probability
            FROM dbo.Embeddings
            WHERE SpatialGeometry IS NOT NULL
              AND EmbeddingId NOT IN (SELECT value FROM STRING_SPLIT(@tokenIds, ','))
            ORDER BY SpatialGeometry.STDistance(@contextCentroid)";

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@tokenIds", tokenIdsStr));
        command.Parameters.Add(new SqlParameter("@temperature", temperature));

        var results = new List<(long, string, double)>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add((
                reader.GetInt64(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.GetDouble(2)
            ));
        }

        return results;
    }

    public async Task<IReadOnlyList<Embedding>> MultiResolutionSearchAsync(
        double queryX,
        double queryY,
        double queryZ,
        int coarseCandidates,
        int fineCandidates,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var sql = $@"
            DECLARE @queryPt GEOMETRY = geometry::STGeomFromText('POINT(' + CAST(@x AS NVARCHAR(50)) + ' ' + CAST(@y AS NVARCHAR(50)) + ')', 0);

            WITH CoarseResults AS (
                SELECT TOP ({coarseCandidates}) EmbeddingId
                FROM dbo.Embeddings
                WHERE SpatialCoarse IS NOT NULL
                ORDER BY SpatialCoarse.STDistance(@queryPt)
            ),
            FineResults AS (
                SELECT TOP ({fineCandidates}) e.EmbeddingId
                FROM dbo.Embeddings e
                INNER JOIN CoarseResults c ON e.EmbeddingId = c.EmbeddingId
                WHERE e.SpatialGeometry IS NOT NULL
                ORDER BY e.SpatialGeometry.STDistance(@queryPt)
            )
            SELECT TOP ({topK}) e.*
            FROM dbo.Embeddings e
            INNER JOIN FineResults f ON e.EmbeddingId = f.EmbeddingId
            ORDER BY e.SpatialGeometry.STDistance(@queryPt)";

        return await _context.Embeddings
            .FromSqlRaw(sql,
                new SqlParameter("@x", queryX),
                new SqlParameter("@y", queryY))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Embedding Embedding, double ActivationStrength, string Level)>> CognitiveActivationAsync(
        float[] queryVector,
        double activationThreshold,
        int maxActivated,
        CancellationToken cancellationToken = default)
    {
        var vectorJson = $"[{string.Join(",", queryVector)}]";

        var sql = $@"
            SELECT TOP ({maxActivated})
                e.*,
                1.0 - VECTOR_DISTANCE('cosine', e.EmbeddingFull, CAST(@vectorJson AS VECTOR(768))) as ActivationStrength,
                CASE
                    WHEN 1.0 - VECTOR_DISTANCE('cosine', e.EmbeddingFull, CAST(@vectorJson AS VECTOR(768))) > 0.95 THEN 'VERY_HIGH'
                    WHEN 1.0 - VECTOR_DISTANCE('cosine', e.EmbeddingFull, CAST(@vectorJson AS VECTOR(768))) > 0.90 THEN 'HIGH'
                    WHEN 1.0 - VECTOR_DISTANCE('cosine', e.EmbeddingFull, CAST(@vectorJson AS VECTOR(768))) > 0.85 THEN 'MEDIUM'
                    ELSE 'LOW'
                END as Level
            FROM dbo.Embeddings e
            WHERE e.EmbeddingFull IS NOT NULL
              AND VECTOR_DISTANCE('cosine', e.EmbeddingFull, CAST(@vectorJson AS VECTOR(768))) < (1.0 - @threshold)
            ORDER BY VECTOR_DISTANCE('cosine', e.EmbeddingFull, CAST(@vectorJson AS VECTOR(768)))";

        var connection = _context.Database.GetDbConnection();
        await _context.Database.OpenConnectionAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@vectorJson", vectorJson));
        command.Parameters.Add(new SqlParameter("@threshold", activationThreshold));

        var results = new List<(Embedding, double, string)>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var embedding = new Embedding
            {
                EmbeddingId = reader.GetInt64(reader.GetOrdinal("EmbeddingId")),
                SourceText = reader.IsDBNull(reader.GetOrdinal("SourceText")) ? null : reader.GetString(reader.GetOrdinal("SourceText")),
                SourceType = reader.GetString(reader.GetOrdinal("SourceType"))
            };

            var strength = reader.GetDouble(reader.GetOrdinal("ActivationStrength"));
            var level = reader.GetString(reader.GetOrdinal("Level"));

            results.Add((embedding, strength, level));
        }

        return results;
    }

    public async Task<string> GenerateTextSpatialAsync(
        string prompt,
        int maxTokens,
        double temperature,
        CancellationToken cancellationToken = default)
    {
        var tokens = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var contextTokenIds = await _context.Embeddings
            .Where(e => tokens.Contains(e.SourceText!))
            .Select(e => e.EmbeddingId)
            .ToListAsync(cancellationToken);

        if (!contextTokenIds.Any()) return prompt;

        var generatedText = prompt;

        for (int i = 0; i < maxTokens; i++)
        {
            var predictions = await PredictNextTokenAsync(contextTokenIds, temperature, 3, cancellationToken);

            if (!predictions.Any()) break;

            var nextToken = predictions.First();
            generatedText += " " + nextToken.Token;
            contextTokenIds.Add(nextToken.TokenId);
        }

        return generatedText;
    }
}
