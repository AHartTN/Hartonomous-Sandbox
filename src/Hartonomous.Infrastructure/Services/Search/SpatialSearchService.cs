using System.Data;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services.Search;

/// <summary>
/// Implements spatial vector search using SQL Server geometry types and spatial indexes.
/// Provides approximate nearest neighbor search by projecting high-dimensional vectors into 3D space.
/// </summary>
public sealed class SpatialSearchService : ISpatialSearchService
{
    private readonly IAtomEmbeddingRepository _atomEmbeddings;
    private readonly ISqlCommandExecutor _sql;
    private readonly ILogger<SpatialSearchService> _logger;

    /// <summary>
    /// Initializes a new spatial search service.
    /// </summary>
    /// <param name="atomEmbeddingRepository">Repository for computing spatial projections.</param>
    /// <param name="sqlCommandExecutor">SQL command executor abstraction.</param>
    /// <param name="logger">Structured logger for diagnostics.</param>
    public SpatialSearchService(
        IAtomEmbeddingRepository atomEmbeddingRepository,
        ISqlCommandExecutor sqlCommandExecutor,
        ILogger<SpatialSearchService> logger)
    {
        _atomEmbeddings = atomEmbeddingRepository ?? throw new ArgumentNullException(nameof(atomEmbeddingRepository));
        _sql = sqlCommandExecutor ?? throw new ArgumentNullException(nameof(sqlCommandExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AtomEmbeddingSearchResult>> SpatialSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing spatial search with topK={TopK}", topK);

        var padded = VectorUtility.PadToSqlLength(queryVector, out _);
        var sqlVector = padded.ToSqlVector();
        var spatialPoint = await _atomEmbeddings
            .ComputeSpatialProjectionAsync(sqlVector, queryVector.Length, cancellationToken)
            .ConfigureAwait(false);

        return await ExecuteSpatialSearchAsync(spatialPoint, topK, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AtomEmbeddingSearchResult>> ExecuteSpatialSearchAsync(
        Point spatialPoint,
        int topK,
        CancellationToken cancellationToken)
    {
        return await _sql.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_ApproxSpatialSearch";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@query_x", spatialPoint.X));
            command.Parameters.Add(new SqlParameter("@query_y", spatialPoint.Y));
            command.Parameters.Add(new SqlParameter("@query_z", spatialPoint.Z));
            command.Parameters.Add(new SqlParameter("@top_k", topK));

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            var results = await reader.ToListAsync(MapSearchResult, token).ConfigureAwait(false);

            _logger.LogInformation("Spatial search returned {Count} results", results.Count);
            return (IReadOnlyList<AtomEmbeddingSearchResult>)results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps SqlDataReader row to AtomEmbeddingSearchResult.
    /// </summary>
    private static AtomEmbeddingSearchResult MapSearchResult(SqlDataReader reader)
    {
        var atomEmbeddingIdOrd = reader.GetOrdinal("AtomEmbeddingId");
        var atomIdOrd = reader.GetOrdinal("AtomId");

        var atomEmbeddingId = reader.GetInt64(atomEmbeddingIdOrd);
        var atomId = reader.GetInt64(atomIdOrd);

        var atom = new Atom
        {
            AtomId = atomId,
            ContentHash = [],
            Modality = reader.GetStringOrNull("Modality") ?? "unknown",
            Subtype = reader.GetStringOrNull("Subtype"),
            SourceUri = reader.GetStringOrNull("SourceUri"),
            SourceType = reader.GetStringOrNull("SourceType"),
            CanonicalText = reader.GetStringOrNull("CanonicalText"),
            Metadata = null,
            CreatedAt = DateTime.UtcNow,
            ReferenceCount = 0,
            IsActive = true
        };

        var embedding = new AtomEmbedding
        {
            AtomEmbeddingId = atomEmbeddingId,
            AtomId = atomId,
            Atom = atom,
            EmbeddingType = reader.GetStringOrNull("EmbeddingType") ?? "default",
            ModelId = TryGetInt32(reader, "ModelId"),
            Dimension = TryGetInt32(reader, "Dimension") ?? 0,
            CreatedAt = reader.GetDateTimeOrNull("CreatedAt") ?? DateTime.UtcNow
        };

        var cosineDistance = reader.GetDoubleOrNull("distance")
            ?? reader.GetDoubleOrNull("ExactDistance")
            ?? reader.GetDoubleOrNull("exact_distance")
            ?? double.NaN;

        var spatialDistance = reader.GetDoubleOrNull("SpatialDistance")
            ?? reader.GetDoubleOrNull("spatial_distance")
            ?? double.NaN;

        return new AtomEmbeddingSearchResult
        {
            Embedding = embedding,
            CosineDistance = cosineDistance,
            SpatialDistance = spatialDistance
        };
    }

    private static int? TryGetInt32(SqlDataReader reader, string columnName)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.GetInt32OrNull(ordinal);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }
}
