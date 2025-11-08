using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Models;
using Hartonomous.Core.Utilities;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    /// Service providing semantic vector search capabilities.
    /// </summary>
    private readonly ISemanticSearchService _semanticSearch;

    /// <summary>
    /// Service providing spatial vector search capabilities.
    /// </summary>
    private readonly ISpatialSearchService _spatialSearch;

    /// <summary>
    /// Service providing semantic feature extraction and analysis.
    /// </summary>
    private readonly ISemanticFeatureService _semanticFeatures;

    /// <summary>
    /// Service providing ensemble inference across multiple models.
    /// </summary>
    private readonly IEnsembleInferenceService _ensembleInference;

    /// <summary>
    /// Service providing text generation capabilities.
    /// </summary>
    private readonly ITextGenerationService _textGeneration;

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
    /// <param name="semanticSearchService">Service for semantic vector search operations.</param>
    /// <param name="spatialSearchService">Service for spatial vector search operations.</param>
    /// <param name="semanticFeatureService">Service for semantic feature extraction.</param>
    /// <param name="ensembleInferenceService">Service for ensemble inference operations.</param>
    /// <param name="textGenerationService">Service for text generation operations.</param>
    /// <param name="logger">Structured logger for diagnostics.</param>
    public InferenceOrchestrator(
        HartonomousDbContext context,
        IAtomEmbeddingRepository atomEmbeddings,
        ISqlCommandExecutor sqlCommandExecutor,
        ISemanticSearchService semanticSearchService,
        ISpatialSearchService spatialSearchService,
        ISemanticFeatureService semanticFeatureService,
        IEnsembleInferenceService ensembleInferenceService,
        ITextGenerationService textGenerationService,
        ILogger<InferenceOrchestrator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _atomEmbeddings = atomEmbeddings ?? throw new ArgumentNullException(nameof(atomEmbeddings));
        _sql = sqlCommandExecutor ?? throw new ArgumentNullException(nameof(sqlCommandExecutor));
        _semanticSearch = semanticSearchService ?? throw new ArgumentNullException(nameof(semanticSearchService));
        _spatialSearch = spatialSearchService ?? throw new ArgumentNullException(nameof(spatialSearchService));
        _semanticFeatures = semanticFeatureService ?? throw new ArgumentNullException(nameof(semanticFeatureService));
        _ensembleInference = ensembleInferenceService ?? throw new ArgumentNullException(nameof(ensembleInferenceService));
        _textGeneration = textGenerationService ?? throw new ArgumentNullException(nameof(textGenerationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an exact semantic vector search using the <c>sp_ExactVectorSearch</c> stored procedure.
    /// </summary>
    /// <param name="queryVector">Vector embedding representing the search query.</param>
    /// <param name="topK">Number of top results to return.</param>
    /// <param name="cancellationToken">Token to cancel command execution.</param>
    /// <returns>Collection of matched embeddings ordered by similarity.</returns>
    public Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
        => _semanticSearch.SemanticSearchAsync(queryVector, topK, cancellationToken);

    /// <summary>
    /// Performs an approximate spatial search by projecting embeddings into vector space geometry.
    /// </summary>
    /// <param name="queryVector">Vector used to compute the spatial projection.</param>
    /// <param name="topK">Number of nearest neighbors to return.</param>
    /// <param name="cancellationToken">Token that can cancel processing.</param>
    /// <returns>Ranked list of embeddings surfaced by the spatial search.</returns>
    public Task<IReadOnlyList<AtomEmbeddingSearchResult>> SpatialSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
        => _spatialSearch.SpatialSearchAsync(queryVector, topK, cancellationToken);

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
        var sqlVector = padded.ToSqlVector();
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
    public Task<EnsembleInferenceResult> EnsembleInferenceAsync(
        string inputData,
        IReadOnlyList<int> modelIds,
        IReadOnlyList<float>? weights = null,
        CancellationToken cancellationToken = default)
        => _ensembleInference.EnsembleInferenceAsync(inputData, modelIds, weights, cancellationToken);

    /// <summary>
    /// Generates text by invoking a spatial search powered stored procedure.
    /// </summary>
    /// <param name="promptEmbedding">Embedding for the generation prompt.</param>
    /// <param name="maxTokens">Maximum tokens to produce.</param>
    /// <param name="temperature">Generation temperature value.</param>
    /// <param name="cancellationToken">Token to cancel SQL execution.</param>
    /// <returns>Generation result with produced text and placeholder metadata.</returns>
    public Task<GenerationResult> GenerateViaSpatialAsync(
        float[] promptEmbedding,
        int maxTokens = 50,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default)
        => _textGeneration.GenerateViaSpatialAsync(promptEmbedding, maxTokens, temperature, cancellationToken);

    /// <summary>
    /// Computes semantic features for the supplied embeddings by calling a stored procedure per embedding.
    /// </summary>
    /// <param name="atomEmbeddingIds">Identifiers of embeddings requiring feature extraction.</param>
    /// <param name="cancellationToken">Token to cancel stored procedure execution.</param>
    /// <returns>Placeholder semantic feature set; production implementation should parse results.</returns>
    public Task<SemanticFeatures> ComputeSemanticFeaturesAsync(
        IReadOnlyList<long> atomEmbeddingIds,
        CancellationToken cancellationToken = default)
        => _semanticFeatures.ComputeSemanticFeaturesAsync(atomEmbeddingIds, cancellationToken);

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
    public Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticFilteredSearchAsync(
        float[] queryVector,
        int topK = 10,
        string? topicFilter = null,
        float? minSentiment = null,
        int? maxAge = null,
        CancellationToken cancellationToken = default)
        => _semanticSearch.SemanticFilteredSearchAsync(queryVector, topK, topicFilter, minSentiment, maxAge, cancellationToken);

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
    /// Reads a 64-bit integer value from the specified column when available.
    /// </summary>
    /// <param name="record">Record containing the column.</param>
    /// <param name="columnName">Name of the column to retrieve.</param>
    /// <returns>Column value or zero when the field is missing or <c>null</c>.</returns>
    private static long GetInt64(SqlDataReader record, string columnName)
    {
        var ordinal = record.GetOrdinalSafe(columnName);
        return record.GetInt64OrNull(ordinal) ?? 0;
    }

    /// <summary>
    /// Reads a string from the given column, returning <c>null</c> when absent.
    /// </summary>
    /// <param name="record">Record containing the column.</param>
    /// <param name="columnName">Name of the column to retrieve.</param>
    /// <returns>String value or <c>null</c> when unavailable.</returns>
    private static string? GetString(SqlDataReader record, string columnName)
    {
        return record.GetStringOrNull(record.GetOrdinalSafe(columnName));
    }

    /// <summary>
    /// Retrieves a nullable 32-bit integer from the specified column.
    /// </summary>
    /// <param name="record">Record containing the data.</param>
    /// <param name="columnName">Column to read.</param>
    /// <returns>Nullable integer with the column contents.</returns>
    private static int? GetNullableInt(SqlDataReader record, string columnName)
    {
        return record.GetInt32OrNull(record.GetOrdinalSafe(columnName));
    }

    /// <summary>
    /// Retrieves a nullable double precision value from the specified column.
    /// </summary>
    /// <param name="record">Record containing the data.</param>
    /// <param name="columnName">Column to read.</param>
    /// <returns>Nullable <see cref="double"/> with the column contents.</returns>
    private static double? GetNullableDouble(SqlDataReader record, string columnName)
    {
        return record.GetDoubleOrNull(record.GetOrdinalSafe(columnName));
    }

    /// <summary>
    /// Retrieves a nullable <see cref="DateTime"/> from the specified column.
    /// </summary>
    /// <param name="record">Record containing the data.</param>
    /// <param name="columnName">Column to read.</param>
    /// <returns>Nullable timestamp with the column contents.</returns>
    private static DateTime? GetNullableDateTime(SqlDataReader record, string columnName)
    {
        return record.GetDateTimeOrNull(record.GetOrdinalSafe(columnName));
    }





    /// <summary>
    /// Invokes a specific model for inference using sp_EnsembleInference.
    /// </summary>
    public async Task<string> InvokeModelAsync(
        int modelId,
        string prompt,
        string? context,
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        // Build input data JSON
        var inputDataBuilder = new StringBuilder("{");
        inputDataBuilder.Append($"\"prompt\":\"{prompt.Replace("\"", "\\\"")}\"");

        if (!string.IsNullOrEmpty(context))
        {
            inputDataBuilder.Append($",\"context\":\"{context.Replace("\"", "\\\"")}\"");
        }

        if (parameters != null && parameters.Count > 0)
        {
            inputDataBuilder.Append(",\"parameters\":{");
            var paramList = parameters.Select(kvp => $"\"{kvp.Key}\":{JsonSerializer.Serialize(kvp.Value)}");
            inputDataBuilder.Append(string.Join(",", paramList));
            inputDataBuilder.Append("}");
        }

        inputDataBuilder.Append("}");

        return await _sql.ExecuteStoredProcedureScalarAsync<string>(
            "dbo.sp_EnsembleInference",
            cancellationToken,
            [
                SqlParam.NVarChar("@inputData", inputDataBuilder.ToString(), -1),
                SqlParam.VarChar("@modelIds", modelId.ToString(), 50),
                SqlParam.VarChar("@taskType", "text-generation", 50)
            ]) ?? string.Empty;
    }
}
