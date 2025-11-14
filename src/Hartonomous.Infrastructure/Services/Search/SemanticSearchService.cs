using System.Data;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services.Search;

/// <summary>
/// Implements semantic vector search using SQL Server stored procedures.
/// Leverages sp_ExactVectorSearch and sp_SemanticSearch for high-performance retrieval.
/// </summary>
public sealed class SemanticSearchService : ISemanticSearchService
{
    private readonly ISqlCommandExecutor _sql;
    private readonly ILogger<SemanticSearchService> _logger;

    /// <summary>
    /// Initializes a new semantic search service.
    /// </summary>
    /// <param name="sqlCommandExecutor">SQL command executor abstraction.</param>
    /// <param name="logger">Structured logger for diagnostics.</param>
    public SemanticSearchService(
        ISqlCommandExecutor sqlCommandExecutor,
        ILogger<SemanticSearchService> logger)
    {
        _sql = sqlCommandExecutor ?? throw new ArgumentNullException(nameof(sqlCommandExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing semantic search with topK={TopK}", topK);

        var padded = VectorUtility.PadToSqlLength(queryVector, out _);
        var vectorParam = new SqlParameter("@query_vector", padded.ToSqlVector());

        return await _sql.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_ExactVectorSearch";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(vectorParam);
            command.Parameters.Add(new SqlParameter("@top_k", topK));

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            var results = await reader.ToListAsync(MapSearchResult, token).ConfigureAwait(false);

            _logger.LogInformation("Semantic search returned {Count} results", results.Count);
            return (IReadOnlyList<AtomEmbeddingSearchResult>)results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticFilteredSearchAsync(
        float[] queryVector,
        int topK = 10,
        string? topicFilter = null,
        float? minSentiment = null,
        int? maxAge = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing filtered semantic search with topK={TopK}, topic={Topic}",
            topK,
            topicFilter);

        var padded = VectorUtility.PadToSqlLength(queryVector, out _);

        return await _sql.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_SemanticSearch";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@query_embedding", padded.ToSqlVector()));
            command.Parameters.Add(new SqlParameter("@query_text", DBNull.Value));
            command.Parameters.Add(new SqlParameter("@top_k", topK));
            command.Parameters.Add(new SqlParameter("@category", (object?)topicFilter ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@use_hybrid", minSentiment.HasValue || maxAge.HasValue ? 1 : 0));

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            var results = await reader.ToListAsync(MapSearchResult, token).ConfigureAwait(false);

            _logger.LogInformation("Filtered search returned {Count} results", results.Count);
            return (IReadOnlyList<AtomEmbeddingSearchResult>)results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps SqlDataReader row to AtomEmbeddingSearchResult using modern extension methods.
    /// </summary>
    private static AtomEmbeddingSearchResult MapSearchResult(SqlDataReader reader)
    {
        // Cache ordinals for performance in list materialization
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

        // Try multiple column name variations for distance metrics
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

    /// <summary>
    /// Safe int32 getter that returns null if column doesn't exist.
    /// SqlDataReaderExtensions throws on missing columns, this method doesn't.
    /// </summary>
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
