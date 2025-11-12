using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Data;

namespace Hartonomous.Data.Repositories;

/// <summary>
/// Repository for vector search operations delegating to SQL Server CLR procedures
/// Calls sp_SpatialVectorSearch, sp_TemporalVectorSearch, sp_HybridSearch, sp_MultiModelEnsemble
/// Uses SQL Server 2025 VECTOR_DISTANCE native functions via stored procedures
/// </summary>
public class VectorSearchRepository : IVectorSearchRepository
{
    private readonly HartonomousDbContext _context;
    private readonly string _connectionString;

    public VectorSearchRepository(HartonomousDbContext context)
    {
        _context = context;
        _connectionString = context.Database.GetConnectionString() 
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    /// <summary>
    /// Performs spatial pre-filtering + exact k-NN search via SQL Server CLR procedure
    /// Delegates to sp_SpatialVectorSearch which uses VECTOR_DISTANCE native function
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult>> SpatialVectorSearchAsync(
        byte[] queryVector,
        Geometry? spatialCenter = null,
        double? radiusMeters = null,
        int topK = 10,
        int tenantId = 0,
        double minSimilarity = 0.0)
    {
        var results = new List<VectorSearchResult>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.sp_SpatialVectorSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@QueryVector", queryVector);
        command.Parameters.AddWithValue("@SpatialCenter", (object?)spatialCenter ?? DBNull.Value);
        command.Parameters.AddWithValue("@RadiusMeters", (object?)radiusMeters ?? DBNull.Value);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@MinSimilarity", minSimilarity);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new VectorSearchResult
            {
                AtomId = reader.GetInt64(reader.GetOrdinal("AtomId")),
                Similarity = reader.GetDouble(reader.GetOrdinal("Similarity")),
                SpatialDistance = reader.GetDouble(reader.GetOrdinal("SpatialDistance")),
                ContentHash = reader.GetFieldValue<byte[]>(reader.GetOrdinal("ContentHash")),
                ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
                CreatedUtc = reader.GetDateTime(reader.GetOrdinal("CreatedUtc"))
            });
        }

        return results;
    }

    /// <summary>
    /// Performs point-in-time semantic search using temporal tables via SQL Server CLR
    /// Delegates to sp_TemporalVectorSearch with FOR SYSTEM_TIME AS OF clause
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult>> TemporalVectorSearchAsync(
        byte[] queryVector,
        DateTime asOfDate,
        int topK = 10,
        int tenantId = 0)
    {
        var results = new List<VectorSearchResult>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.sp_TemporalVectorSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@QueryVector", queryVector);
        command.Parameters.AddWithValue("@AsOfDate", asOfDate);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new VectorSearchResult
            {
                AtomId = reader.GetInt64(reader.GetOrdinal("AtomId")),
                Similarity = reader.GetDouble(reader.GetOrdinal("Similarity")),
                ContentHash = reader.GetFieldValue<byte[]>(reader.GetOrdinal("ContentHash")),
                ContentType = reader.GetString(reader.GetOrdinal("ContentType")),
                CreatedUtc = reader.GetDateTime(reader.GetOrdinal("LastComputedUtc"))
            });
        }

        return results;
    }

    /// <summary>
    /// Performs hybrid search combining vector and spatial ranking via SQL Server CLR
    /// Delegates to sp_HybridSearch which uses VECTOR_DISTANCE native function + spatial STDistance
    /// sp_HybridSearch returns results already ranked by vector distance; spatial_distance is informational
    /// </summary>
    public async Task<IReadOnlyList<HybridSearchResult>> HybridSearchAsync(
        byte[] queryVector,
        string? keywords = null,
        Geometry? spatialRegion = null,
        int topK = 10,
        double vectorWeight = 0.5,
        double keywordWeight = 0.3,
        double spatialWeight = 0.2,
        int tenantId = 0)
    {
        var results = new List<HybridSearchResult>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.sp_HybridSearch", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        // Extract spatial point from region (if provided)
        double? spatialX = null, spatialY = null, spatialZ = null;
        if (spatialRegion is Point point)
        {
            spatialX = point.X;
            spatialY = point.Y;
            spatialZ = point.Z;
        }

        command.Parameters.AddWithValue("@query_vector", queryVector);
        command.Parameters.AddWithValue("@query_dimension", queryVector.Length);
        command.Parameters.AddWithValue("@query_spatial_x", (object?)spatialX ?? DBNull.Value);
        command.Parameters.AddWithValue("@query_spatial_y", (object?)spatialY ?? DBNull.Value);
        command.Parameters.AddWithValue("@query_spatial_z", (object?)spatialZ ?? DBNull.Value);
        command.Parameters.AddWithValue("@spatial_candidates", 100);
        command.Parameters.AddWithValue("@final_top_k", topK);
        command.Parameters.AddWithValue("@distance_metric", "cosine");
        command.Parameters.AddWithValue("@embedding_type", DBNull.Value);
        command.Parameters.AddWithValue("@ModelId", DBNull.Value);
        command.Parameters.AddWithValue("@srid", 0);

        using var reader = await command.ExecuteReaderAsync();
        
        // sp_HybridSearch performs:
        // 1. Spatial filter (STDistance) → ~100 candidates
        // 2. Exact vector rerank (VECTOR_DISTANCE) → top K
        // Results are pre-ranked by exact_distance (cosine)
        int rank = 1;
        while (await reader.ReadAsync())
        {
            var exactDistance = reader.GetDouble(reader.GetOrdinal("exact_distance"));
            var spatialDistance = reader.GetDouble(reader.GetOrdinal("spatial_distance"));
            
            var vectorScore = 1.0 - exactDistance; // Cosine similarity
            
            // Normalize spatial distance to 0-1 score (inverse distance)
            // Assume max meaningful spatial distance is 1000 units
            var spatialScore = spatialDistance > 0 ? Math.Max(0, 1.0 - (spatialDistance / 1000.0)) : 1.0;
            
            // Weighted combination (no RRF needed - already ranked by stored proc)
            var combinedScore = (vectorScore * vectorWeight) + (spatialScore * spatialWeight);
            
            results.Add(new HybridSearchResult
            {
                AtomId = reader.GetInt64(reader.GetOrdinal("AtomId")),
                VectorScore = vectorScore,
                KeywordScore = 0.0, // Keywords not implemented in sp_HybridSearch yet
                SpatialScore = spatialScore,
                CombinedScore = combinedScore,
                ContentHash = null, // Not returned by sp_HybridSearch
                ContentType = reader.GetString(reader.GetOrdinal("Modality")),
                CreatedUtc = DateTime.UtcNow // Not returned by sp_HybridSearch
            });
            
            rank++;
        }

        return results;
    }

    /// <summary>
    /// Performs ensemble search blending results from multiple models via SQL Server CLR
    /// Delegates to sp_MultiModelEnsemble which uses VECTOR_DISTANCE for each model
    /// </summary>
    public async Task<IReadOnlyList<EnsembleSearchResult>> MultiModelEnsembleSearchAsync(
        byte[] queryVector1, byte[] queryVector2, byte[] queryVector3,
        int model1Id, int model2Id, int model3Id,
        double model1Weight = 0.4, double model2Weight = 0.35, double model3Weight = 0.25,
        int topK = 10, int tenantId = 0)
    {
        var results = new List<EnsembleSearchResult>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.sp_MultiModelEnsemble", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@QueryVector1", queryVector1);
        command.Parameters.AddWithValue("@QueryVector2", queryVector2);
        command.Parameters.AddWithValue("@QueryVector3", queryVector3);
        command.Parameters.AddWithValue("@Model1Id", model1Id);
        command.Parameters.AddWithValue("@Model2Id", model2Id);
        command.Parameters.AddWithValue("@Model3Id", model3Id);
        command.Parameters.AddWithValue("@Model1Weight", model1Weight);
        command.Parameters.AddWithValue("@Model2Weight", model2Weight);
        command.Parameters.AddWithValue("@Model3Weight", model3Weight);
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new EnsembleSearchResult
            {
                AtomId = reader.GetInt64(reader.GetOrdinal("AtomId")),
                Model1Score = reader.GetDouble(reader.GetOrdinal("Model1Score")),
                Model2Score = reader.GetDouble(reader.GetOrdinal("Model2Score")),
                Model3Score = reader.GetDouble(reader.GetOrdinal("Model3Score")),
                EnsembleScore = reader.GetDouble(reader.GetOrdinal("EnsembleScore")),
                ContentHash = reader.GetFieldValue<byte[]>(reader.GetOrdinal("ContentHash")),
                ContentType = reader.GetString(reader.GetOrdinal("ContentType"))
            });
        }

        return results;
    }
}
