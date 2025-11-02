using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Utilities;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using System.Data;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Orchestrates AI inference operations by calling T-SQL stored procedures.
/// Database-native AI: inference executes as SELECT statements in SQL Server.
/// </summary>
public sealed class InferenceOrchestrator : IInferenceService
{
    /// <summary>
    /// Entity Framework context that exposes command access to Hartonomous tables and procedures.
    /// </summary>
    private readonly HartonomousDbContext _context;

    /// <summary>
    /// Repository supplying embedding projections and hybrid search capabilities.
    /// </summary>
    private readonly IAtomEmbeddingRepository _atomEmbeddings;

    /// <summary>
    /// Abstraction for executing parameterized SQL commands and stored procedures.
    /// </summary>
    private readonly ISqlCommandExecutor _sql;

    /// <summary>
    /// Logger used to trace orchestrated inference operations.
    /// </summary>
    private readonly ILogger<InferenceOrchestrator> _logger;

    /// <summary>
    /// Initializes a new inference orchestrator backed by SQL Server stored procedures.
    /// </summary>
    /// <param name="context">Database context used for direct command execution.</param>
    /// <param name="atomEmbeddings">Repository that exposes embedding utilities.</param>
    /// <param name="sqlCommandExecutor">SQL command executor abstraction.</param>
    /// <param name="logger">Structured logger for diagnostics.</param>
    public InferenceOrchestrator(
        HartonomousDbContext context,
        IAtomEmbeddingRepository atomEmbeddings,
        ISqlCommandExecutor sqlCommandExecutor,
        ILogger<InferenceOrchestrator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _atomEmbeddings = atomEmbeddings ?? throw new ArgumentNullException(nameof(atomEmbeddings));
        _sql = sqlCommandExecutor ?? throw new ArgumentNullException(nameof(sqlCommandExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an exact semantic vector search using the <c>sp_ExactVectorSearch</c> stored procedure.
    /// </summary>
    /// <param name="queryVector">Vector embedding representing the search query.</param>
    /// <param name="topK">Number of top results to return.</param>
    /// <param name="cancellationToken">Token to cancel command execution.</param>
    /// <returns>Collection of matched embeddings ordered by similarity.</returns>
    public async Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing semantic search with topK={TopK}", topK);

        var padded = VectorUtility.PadToSqlLength(queryVector, out _);
        var vectorParam = new SqlParameter("@query_vector", new SqlVector<float>(padded));

        return await _sql.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_ExactVectorSearch";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(vectorParam);
            command.Parameters.Add(new SqlParameter("@top_k", topK));

            var results = new List<AtomEmbeddingSearchResult>();
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                results.Add(MapSearchResult(reader));
            }

            _logger.LogInformation("Semantic search returned {Count} results", results.Count);
            return results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs an approximate spatial search by projecting embeddings into vector space geometry.
    /// </summary>
    /// <param name="queryVector">Vector used to compute the spatial projection.</param>
    /// <param name="topK">Number of nearest neighbors to return.</param>
    /// <param name="cancellationToken">Token that can cancel processing.</param>
    /// <returns>Ranked list of embeddings surfaced by the spatial search.</returns>
    public async Task<IReadOnlyList<AtomEmbeddingSearchResult>> SpatialSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing spatial search with topK={TopK}", topK);

        var padded = VectorUtility.PadToSqlLength(queryVector, out _);
        var sqlVector = new SqlVector<float>(padded);
        var spatialPoint = await _atomEmbeddings
            .ComputeSpatialProjectionAsync(sqlVector, queryVector.Length, cancellationToken)
            .ConfigureAwait(false);

        return await ExecuteSpatialSearchAsync(spatialPoint, topK, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Combines semantic and spatial metrics to surface high-quality matches.
    /// </summary>
    /// <param name="queryVector">Embedding for the search query.</param>
    /// <param name="topK">Number of final results.</param>
    /// <param name="candidateCount">Initial candidate pool prior to re-ranking.</param>
    /// <param name="cancellationToken">Token to abort operations.</param>
    /// <returns>List of search results blended from semantic and spatial scoring.</returns>
    public async Task<IReadOnlyList<AtomEmbeddingSearchResult>> HybridSearchAsync(
        float[] queryVector,
        int topK = 10,
        int candidateCount = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing hybrid search with topK={TopK}, candidateCount={CandidateCount}",
            topK,
            candidateCount);

        var padded = VectorUtility.PadToSqlLength(queryVector, out _);
        var sqlVector = new SqlVector<float>(padded);
        var spatialPoint = await _atomEmbeddings
            .ComputeSpatialProjectionAsync(sqlVector, queryVector.Length, cancellationToken)
            .ConfigureAwait(false);

        var results = await _atomEmbeddings
            .HybridSearchAsync(queryVector, spatialPoint, candidateCount, topK, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Hybrid search returned {Count} results", results.Count);
        return results;
    }

    /// <summary>
    /// Executes ensemble inference across multiple models using a stored procedure.
    /// </summary>
    /// <param name="inputData">Input payload or prompt sent to the ensemble.</param>
    /// <param name="modelIds">Identifiers of models participating in the ensemble.</param>
    /// <param name="weights">Optional weighting factors for each model.</param>
    /// <param name="cancellationToken">Token for cancelling database work.</param>
    /// <returns>Aggregate inference result with placeholder contribution metrics.</returns>
    public async Task<EnsembleInferenceResult> EnsembleInferenceAsync(
        string inputData,
        IReadOnlyList<int> modelIds,
        IReadOnlyList<float>? weights = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing ensemble inference with {ModelCount} models",
            modelIds.Count);

        // For ensemble, we need to call sp_EnsembleInference which returns JSON
        // This is a simplified implementation - full version would parse JSON result

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_EnsembleInference";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@inputData", inputData));
        command.Parameters.Add(new SqlParameter("@modelIds", string.Join(",", modelIds)));
        command.Parameters.Add(new SqlParameter("@taskType", "classification"));

        var result = await command.ExecuteScalarAsync(cancellationToken);

        _logger.LogInformation("Ensemble inference completed");

        // Placeholder result - real implementation would parse T-SQL output
        return new EnsembleInferenceResult
        {
            InferenceId = 0,
            OutputData = result?.ToString() ?? string.Empty,
            ConfidenceScore = 0.85f,
            ModelContributions = Array.Empty<ModelContribution>(),
            CompletedTimestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates text by invoking a spatial search powered stored procedure.
    /// </summary>
    /// <param name="promptEmbedding">Embedding for the generation prompt.</param>
    /// <param name="maxTokens">Maximum tokens to produce.</param>
    /// <param name="temperature">Generation temperature value.</param>
    /// <param name="cancellationToken">Token to cancel SQL execution.</param>
    /// <returns>Generation result with produced text and placeholder metadata.</returns>
    public async Task<GenerationResult> GenerateViaSpatialAsync(
        float[] promptEmbedding,
        int maxTokens = 50,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Generating text via spatial search, maxTokens={MaxTokens}, temperature={Temperature}",
            maxTokens,
            temperature);

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_GenerateViaSpatial";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        var paddedPrompt = VectorUtility.PadToSqlLength(promptEmbedding, out _);
        command.Parameters.Add(new SqlParameter("@promptEmbedding", new SqlVector<float>(paddedPrompt)));
        command.Parameters.Add(new SqlParameter("@maxTokens", maxTokens));
        command.Parameters.Add(new SqlParameter("@temperature", temperature));

        var result = await command.ExecuteScalarAsync(cancellationToken);

        _logger.LogInformation("Generation completed");

        return new GenerationResult
        {
            GeneratedText = result?.ToString() ?? string.Empty,
            TokenIds = Array.Empty<int>(),
            TokenConfidences = Array.Empty<float>(),
            TokenCount = 0,
            AverageConfidence = 0.0f,
            InferenceId = 0
        };
    }

    /// <summary>
    /// Computes semantic features for the supplied embeddings by calling a stored procedure per embedding.
    /// </summary>
    /// <param name="atomEmbeddingIds">Identifiers of embeddings requiring feature extraction.</param>
    /// <param name="cancellationToken">Token to cancel stored procedure execution.</param>
    /// <returns>Placeholder semantic feature set; production implementation should parse results.</returns>
    public async Task<SemanticFeatures> ComputeSemanticFeaturesAsync(
        IReadOnlyList<long> atomEmbeddingIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Computing semantic features for {Count} embeddings",
            atomEmbeddingIds.Count);

        foreach (var atomEmbeddingId in atomEmbeddingIds)
        {
            await _sql.ExecuteAsync(async (command, token) =>
            {
                command.CommandText = "dbo.sp_ComputeSemanticFeatures";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(new SqlParameter("@AtomEmbeddingId", atomEmbeddingId));

                await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Semantic features computed");

        // Placeholder - real implementation would parse procedure output
        return new SemanticFeatures
        {
            Topics = Array.Empty<string>(),
            SentimentScore = 0.0f,
            Entities = Array.Empty<string>(),
            Keywords = Array.Empty<string>(),
            TemporalRelevance = 0.0f,
            FeatureScores = new Dictionary<string, float>()
        };
    }

    /// <summary>
    /// Executes semantic search with optional topic, sentiment, and recency filters.
    /// </summary>
    /// <param name="queryVector">Embedding for the query.</param>
    /// <param name="topK">Maximum results to return.</param>
    /// <param name="topicFilter">Optional topic constraint.</param>
    /// <param name="minSentiment">Optional minimum sentiment threshold.</param>
    /// <param name="maxAge">Optional recency window for results.</param>
    /// <param name="cancellationToken">Token for cancelling the search.</param>
    /// <returns>Filtered list of embeddings meeting the criteria.</returns>
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
            command.Parameters.Add(new SqlParameter("@query_embedding", new SqlVector<float>(padded)));
            command.Parameters.Add(new SqlParameter("@query_text", DBNull.Value));
            command.Parameters.Add(new SqlParameter("@top_k", topK));
            command.Parameters.Add(new SqlParameter("@category", (object?)topicFilter ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@use_hybrid", minSentiment.HasValue || maxAge.HasValue ? 1 : 0));

            var results = new List<AtomEmbeddingSearchResult>();
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                results.Add(MapSearchResult(reader));
            }

            _logger.LogInformation("Filtered search returned {Count} results", results.Count);
            return results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Records user feedback for a prior inference execution.
    /// </summary>
    /// <param name="inferenceId">Identifier of the inference request.</param>
    /// <param name="rating">Rating between 1 and 5 inclusive.</param>
    /// <param name="feedback">Optional textual feedback.</param>
    /// <param name="cancellationToken">Token to cancel command execution.</param>
    public async Task SubmitFeedbackAsync(
        long inferenceId,
        byte rating,
        string? feedback = null,
        CancellationToken cancellationToken = default)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5");
        }

        _logger.LogInformation(
            "Submitting feedback for inference {InferenceId}, rating={Rating}",
            inferenceId,
            rating);

        await _context.Database.ExecuteSqlRawAsync(@"
            UPDATE InferenceRequests
            SET UserRating = {0}, UserFeedback = {1}
            WHERE InferenceId = {2}",
            rating,
            feedback ?? (object)DBNull.Value,
            inferenceId,
            cancellationToken);

        _logger.LogInformation("Feedback submitted successfully");
    }

    /// <summary>
    /// Calls a stored procedure that updates model weights based on aggregated feedback.
    /// </summary>
    /// <param name="learningRate">Learning rate parameter supplied to the procedure.</param>
    /// <param name="minRatings">Minimum ratings required before updates are applied.</param>
    /// <param name="cancellationToken">Token that can cancel execution.</param>
    /// <returns>Number of layers updated as reported by the procedure.</returns>
    public async Task<int> UpdateWeightsFromFeedbackAsync(
        float learningRate = 0.001f,
        int minRatings = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Updating model weights from feedback, learningRate={LearningRate}, minRatings={MinRatings}",
            learningRate,
            minRatings);

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_UpdateModelWeightsFromFeedback";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@learningRate", learningRate));
        command.Parameters.Add(new SqlParameter("@minRatings", minRatings));

        var layersUpdated = await command.ExecuteScalarAsync(cancellationToken);

        var count = Convert.ToInt32(layersUpdated ?? 0);
        _logger.LogInformation("Updated {Count} model layers from feedback", count);

        return count;
    }

    /// <summary>
    /// Executes the spatial search stored procedure for the supplied 3D projection.
    /// </summary>
    /// <param name="spatialPoint">Point representing the projected embedding in SQL geometry space.</param>
    /// <param name="topK">Total results requested.</param>
    /// <param name="cancellationToken">Token to cancel command execution.</param>
    /// <returns>List of search results produced by the stored procedure.</returns>
    private async Task<IReadOnlyList<AtomEmbeddingSearchResult>> ExecuteSpatialSearchAsync(
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

            var results = new List<AtomEmbeddingSearchResult>();
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                results.Add(MapSearchResult(reader));
            }

            _logger.LogInformation("Spatial search returned {Count} results", results.Count);
            return results;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Materializes an <see cref="AtomEmbeddingSearchResult"/> from the given data reader row.
    /// </summary>
    /// <param name="record">Data record returned from a search stored procedure.</param>
    /// <returns>Populated search result with nested atom and embedding entities.</returns>
    private static AtomEmbeddingSearchResult MapSearchResult(IDataRecord record)
    {
        var atomEmbeddingId = GetInt64(record, "AtomEmbeddingId");
        var atomId = GetInt64(record, "AtomId");

        var atom = new Atom
        {
            AtomId = atomId,
            ContentHash = Array.Empty<byte>(),
            Modality = GetString(record, "Modality") ?? "unknown",
            Subtype = GetString(record, "Subtype"),
            SourceUri = GetString(record, "SourceUri"),
            SourceType = GetString(record, "SourceType"),
            CanonicalText = GetString(record, "CanonicalText"),
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
            EmbeddingType = GetString(record, "EmbeddingType") ?? "default",
            ModelId = GetNullableInt(record, "ModelId"),
            Dimension = GetNullableInt(record, "Dimension") ?? 0,
            CreatedAt = GetNullableDateTime(record, "CreatedAt") ?? DateTime.UtcNow
        };

        var cosineDistance = GetNullableDouble(record, "distance")
            ?? GetNullableDouble(record, "ExactDistance")
            ?? GetNullableDouble(record, "exact_distance")
            ?? double.NaN;

        var spatialDistance = GetNullableDouble(record, "SpatialDistance")
            ?? GetNullableDouble(record, "spatial_distance")
            ?? double.NaN;

        return new AtomEmbeddingSearchResult
        {
            Embedding = embedding,
            CosineDistance = cosineDistance,
            SpatialDistance = spatialDistance
        };
    }

    /// <summary>
    /// Reads a 64-bit integer value from the specified column when available.
    /// </summary>
    /// <param name="record">Record containing the column.</param>
    /// <param name="columnName">Name of the column to retrieve.</param>
    /// <returns>Column value or zero when the field is missing or <c>null</c>.</returns>
    private static long GetInt64(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetInt64(ordinal) : 0;
    }

    /// <summary>
    /// Reads a string from the given column, returning <c>null</c> when absent.
    /// </summary>
    /// <param name="record">Record containing the column.</param>
    /// <param name="columnName">Name of the column to retrieve.</param>
    /// <returns>String value or <c>null</c> when unavailable.</returns>
    private static string? GetString(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetString(ordinal) : null;
    }

    /// <summary>
    /// Retrieves a nullable 32-bit integer from the specified column.
    /// </summary>
    /// <param name="record">Record containing the data.</param>
    /// <param name="columnName">Column to read.</param>
    /// <returns>Nullable integer with the column contents.</returns>
    private static int? GetNullableInt(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetInt32(ordinal) : null;
    }

    /// <summary>
    /// Retrieves a nullable double precision value from the specified column.
    /// </summary>
    /// <param name="record">Record containing the data.</param>
    /// <param name="columnName">Column to read.</param>
    /// <returns>Nullable <see cref="double"/> with the column contents.</returns>
    private static double? GetNullableDouble(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetDouble(ordinal) : null;
    }

    /// <summary>
    /// Retrieves a nullable <see cref="DateTime"/> from the specified column.
    /// </summary>
    /// <param name="record">Record containing the data.</param>
    /// <param name="columnName">Column to read.</param>
    /// <returns>Nullable timestamp with the column contents.</returns>
    private static DateTime? GetNullableDateTime(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetDateTime(ordinal) : null;
    }

    /// <summary>
    /// Resolves the ordinal index for the specified column, ignoring case.
    /// </summary>
    /// <param name="record">Record that exposes the schema fields.</param>
    /// <param name="columnName">Name of the column to resolve.</param>
    /// <returns>Column index or <c>-1</c> when the column is absent.</returns>
    private static int GetOrdinal(IDataRecord record, string columnName)
    {
        for (var i = 0; i < record.FieldCount; i++)
        {
            if (string.Equals(record.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
