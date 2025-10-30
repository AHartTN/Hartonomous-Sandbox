using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Generic repository for dimension-specific weight tables.
/// Supports index-only queries, bulk inserts, and vector similarity search.
/// </summary>
public class WeightRepository<TWeight> : IWeightRepository<TWeight>
    where TWeight : WeightBase
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<WeightRepository<TWeight>> _logger;
    private readonly DbSet<TWeight> _dbSet;
    private readonly string _tableName;

    public WeightRepository(
        HartonomousDbContext context,
        ILogger<WeightRepository<TWeight>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbSet = _context.Set<TWeight>();

        // Determine table name from entity type
        _tableName = typeof(TWeight).Name switch
        {
            nameof(Weight768) => "Weights_768",
            nameof(Weight1536) => "Weights_1536",
            nameof(Weight1998) => "Weights_1998",
            nameof(Weight3996) => "Weights_3996",
            _ => throw new InvalidOperationException($"Unsupported weight type: {typeof(TWeight).Name}")
        };
    }

    public async Task<TWeight?> GetByIdAsync(long weightId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { weightId }, cancellationToken);
    }

    public async Task<IReadOnlyList<TWeight>> GetByModelAsync(
        int modelId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(w => w.ModelId == modelId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TWeight>> GetByModelAndLayersAsync(
        int modelId,
        IEnumerable<int> layerIndices,
        float? minImportance = null,
        CancellationToken cancellationToken = default)
    {
        var layerList = layerIndices.ToList();

        var query = _dbSet
            .Where(w => w.ModelId == modelId && layerList.Contains(w.LayerIdx));

        if (minImportance.HasValue)
        {
            query = query.Where(w => w.ImportanceScore >= minImportance.Value);
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TWeight>> GetTopImportantWeightsAsync(
        int modelId,
        int topN,
        IEnumerable<int>? layerIndices = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(w => w.ModelId == modelId && w.ImportanceScore != null);

        if (layerIndices != null)
        {
            var layerList = layerIndices.ToList();
            query = query.Where(w => layerList.Contains(w.LayerIdx));
        }

        return await query
            .OrderByDescending(w => w.ImportanceScore)
            .Take(topN)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<TWeight> AddAsync(TWeight weight, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(weight, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return weight;
    }

    public async Task BulkInsertAsync(IEnumerable<TWeight> weights, CancellationToken cancellationToken = default)
    {
        // Use EF Core bulk extensions for performance
        // Falls back to AddRange if bulk extensions not available
        var weightList = weights.ToList();

        _logger.LogInformation("Bulk inserting {Count} weights into {Table}", weightList.Count, _tableName);

        // For now, use AddRange (TODO: Implement SqlBulkCopy or EFCore.BulkExtensions)
        await _dbSet.AddRangeAsync(weightList, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bulk insert completed for {Count} weights", weightList.Count);
    }

    public async Task UpdateImportanceScoresAsync(
        IDictionary<long, float> weightScores,
        CancellationToken cancellationToken = default)
    {
        foreach (var (weightId, score) in weightScores)
        {
            var weight = await _dbSet.FindAsync(new object[] { weightId }, cancellationToken);
            if (weight != null)
            {
                weight.ImportanceScore = score;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(TWeight Weight, float Distance)>> FindSimilarWeightsExactAsync(
        string queryVectorJson,
        int topK,
        int? modelId = null,
        CancellationToken cancellationToken = default)
    {
        // Use VECTOR_DISTANCE for exact nearest neighbor search
        var sql = $@"
            SELECT TOP (@topK)
                weight_id,
                model_id,
                layer_idx,
                component_type,
                head_idx,
                from_position,
                to_position,
                weight_vector,
                importance_score,
                last_updated,
                VECTOR_DISTANCE('cosine', weight_vector, @queryVector) AS distance
            FROM dbo.{_tableName}
            WHERE (@modelId IS NULL OR model_id = @modelId)
            ORDER BY VECTOR_DISTANCE('cosine', weight_vector, @queryVector)";

        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@topK", topK));
        command.Parameters.Add(new SqlParameter("@queryVector", queryVectorJson));
        command.Parameters.Add(new SqlParameter("@modelId", (object?)modelId ?? DBNull.Value));

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var results = new List<(TWeight, float)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var weight = MapFromReader(reader);
            var distance = reader.GetFloat(reader.GetOrdinal("distance"));
            results.Add((weight, distance));
        }

        return results;
    }

    public async Task<IReadOnlyList<(TWeight Weight, float Distance)>> FindSimilarWeightsApproximateAsync(
        string queryVectorJson,
        int topK,
        int? modelId = null,
        CancellationToken cancellationToken = default)
    {
        // Use VECTOR_SEARCH with DiskANN index
        // Note: Requires VECTOR INDEX to be created on table
        var sql = $@"
            SELECT
                weight_id,
                model_id,
                layer_idx,
                component_type,
                head_idx,
                from_position,
                to_position,
                weight_vector,
                importance_score,
                last_updated,
                distance
            FROM VECTOR_SEARCH(
                'dbo.{_tableName}',
                'weight_vector',
                @queryVector,
                @topK,
                @additionalParams
            ) AS results";

        var additionalParams = modelId.HasValue
            ? $"WHERE model_id = {modelId.Value}"
            : "";

        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@queryVector", queryVectorJson));
        command.Parameters.Add(new SqlParameter("@topK", topK));
        command.Parameters.Add(new SqlParameter("@additionalParams", additionalParams));

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var results = new List<(TWeight, float)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var weight = MapFromReader(reader);
            var distance = reader.GetFloat(reader.GetOrdinal("distance"));
            results.Add((weight, distance));
        }

        return results;
    }

    public async Task<int> GetCountByModelAsync(int modelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(w => w.ModelId == modelId)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> HasVectorIndexAsync(CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(@tableName)
            AND type_desc = 'VECTOR'";

        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new SqlParameter("@tableName", $"dbo.{_tableName}"));

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var count = (int?)await command.ExecuteScalarAsync(cancellationToken);
        return count > 0;
    }

    private TWeight MapFromReader(System.Data.Common.DbDataReader reader)
    {
        var weight = Activator.CreateInstance<TWeight>();

        weight.WeightId = reader.GetInt64(reader.GetOrdinal("weight_id"));
        weight.ModelId = reader.GetInt32(reader.GetOrdinal("model_id"));
        weight.LayerIdx = reader.GetInt32(reader.GetOrdinal("layer_idx"));
        weight.ComponentType = reader.GetString(reader.GetOrdinal("component_type"));

        var headIdxOrdinal = reader.GetOrdinal("head_idx");
        weight.HeadIdx = reader.IsDBNull(headIdxOrdinal) ? null : reader.GetInt32(headIdxOrdinal);

        var fromPosOrdinal = reader.GetOrdinal("from_position");
        weight.FromPosition = reader.IsDBNull(fromPosOrdinal) ? null : reader.GetInt32(fromPosOrdinal);

        var toPosOrdinal = reader.GetOrdinal("to_position");
        weight.ToPosition = reader.IsDBNull(toPosOrdinal) ? null : reader.GetInt32(toPosOrdinal);

        weight.WeightVectorJson = reader.GetString(reader.GetOrdinal("weight_vector"));

        var importanceOrdinal = reader.GetOrdinal("importance_score");
        weight.ImportanceScore = reader.IsDBNull(importanceOrdinal) ? null : reader.GetFloat(importanceOrdinal);

        weight.LastUpdated = reader.GetDateTime(reader.GetOrdinal("last_updated"));

        return weight;
    }
}
