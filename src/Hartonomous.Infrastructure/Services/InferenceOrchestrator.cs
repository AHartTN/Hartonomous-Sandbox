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
using System.Globalization;
using System.Linq;
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
        var vectorParam = new SqlParameter("@query_vector", padded.ToSqlVector());

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
        var sqlVector = padded.ToSqlVector();
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
    public async Task<EnsembleInferenceResult> EnsembleInferenceAsync(
        string inputData,
        IReadOnlyList<int> modelIds,
        IReadOnlyList<float>? weights = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing ensemble inference with {ModelCount} models",
            modelIds.Count);

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        long inferenceId = 0;
        var resultRows = new List<EnsembleAtomScore>();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "dbo.sp_EnsembleInference";
            command.CommandType = System.Data.CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@inputData", inputData));
            command.Parameters.Add(new SqlParameter("@modelIds", string.Join(",", modelIds)));
            command.Parameters.Add(new SqlParameter("@taskType", "classification"));

            // Execute and read result set
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                // First row contains InferenceId
                if (inferenceId == 0)
                    inferenceId = reader.GetInt64(reader.GetOrdinal("InferenceId"));

                resultRows.Add(new EnsembleAtomScore
                {
                    AtomEmbeddingId = reader.GetInt64(reader.GetOrdinal("AtomEmbeddingId")),
                    AtomId = reader.GetInt64(reader.GetOrdinal("AtomId")),
                    CanonicalText = reader.IsDBNull(reader.GetOrdinal("CanonicalText")) 
                        ? string.Empty 
                        : reader.GetString(reader.GetOrdinal("CanonicalText")),
                    ModelCount = reader.GetInt32(reader.GetOrdinal("ModelCount")),
                    EnsembleScore = reader.GetDouble(reader.GetOrdinal("EnsembleScore")),
                    IsConsensus = reader.GetInt32(reader.GetOrdinal("IsConsensus")) == 1
                });
            }
        }

        // Query InferenceRequests table to get metadata and steps
        var inferenceRequest = await _context.InferenceRequests
            .Include(i => i.Steps)
                .ThenInclude(s => s.Model)
            .FirstOrDefaultAsync(i => i.InferenceId == inferenceId, cancellationToken);

        if (inferenceRequest == null)
        {
            _logger.LogWarning("InferenceRequest {InferenceId} not found after ensemble execution", inferenceId);
            return new EnsembleInferenceResult
            {
                InferenceId = inferenceId,
                OutputData = JsonSerializer.Serialize(resultRows),
                ConfidenceScore = 0.0f,
                ModelContributions = Array.Empty<ModelContribution>(),
                CompletedTimestamp = DateTime.UtcNow
            };
        }

        // Calculate confidence from consensus
        var totalModels = inferenceRequest.Steps.Count;
        var consensusCount = resultRows.Count(r => r.IsConsensus);
        var confidence = totalModels > 0 && resultRows.Count > 0
            ? (float)consensusCount / resultRows.Count
            : 0.0f;

        // Build ModelContributions from InferenceSteps
        var contributions = inferenceRequest.Steps
            .Where(s => s.Model != null)
            .Select(step => new ModelContribution
            {
                ModelId = step.ModelId ?? 0,
                ModelName = step.Model?.ModelName ?? "Unknown",
                IndividualOutput = $"Step {step.StepNumber}: {step.OperationType}",
                Weight = 1.0f / totalModels, // Equal weight for now (TODO: parse from ModelsUsed JSON)
                ConfidenceScore = step.DurationMs.HasValue && inferenceRequest.TotalDurationMs.HasValue
                    ? 1.0f - ((float)step.DurationMs.Value / inferenceRequest.TotalDurationMs.Value)
                    : 0.5f
            })
            .ToList();

        _logger.LogInformation(
            "Ensemble inference completed: InferenceId={InferenceId}, Confidence={Confidence:F2}, Results={ResultCount}",
            inferenceId, confidence, resultRows.Count);

        return new EnsembleInferenceResult
        {
            InferenceId = inferenceId,
            OutputData = JsonSerializer.Serialize(resultRows.Take(10)), // Top 10 results
            ConfidenceScore = confidence,
            ModelContributions = contributions,
            CompletedTimestamp = DateTime.UtcNow
        };
    }

    // Helper class for parsing ensemble results
    private sealed class EnsembleAtomScore
    {
        public long AtomEmbeddingId { get; init; }
        public long AtomId { get; init; }
        public string CanonicalText { get; init; } = string.Empty;
        public int ModelCount { get; init; }
        public double EnsembleScore { get; init; }
        public bool IsConsensus { get; init; }
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

        if (promptEmbedding is null || promptEmbedding.Length == 0)
        {
            throw new ArgumentException("Prompt embedding must contain at least one value", nameof(promptEmbedding));
        }

        var padded = VectorUtility.PadToSqlLength(promptEmbedding, out _);
        var sqlVector = padded.ToSqlVector();
        var spatialPoint = await _atomEmbeddings
            .ComputeSpatialProjectionAsync(sqlVector, promptEmbedding.Length, cancellationToken)
            .ConfigureAwait(false);

        // Use hybrid search to recover representative context terms for the prompt.
        var nearestContext = await _atomEmbeddings
            .HybridSearchAsync(promptEmbedding, spatialPoint, 32, 8, cancellationToken)
            .ConfigureAwait(false);

        var promptTokens = nearestContext
            .Select(result => result.Embedding?.Atom?.CanonicalText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        if (promptTokens.Count == 0)
        {
            // Fall back to a numeric fingerprint so the stored procedure receives a non-empty prompt.
            var fallback = string.Join(
                ' ',
                promptEmbedding
                    .Take(Math.Min(16, promptEmbedding.Length))
                    .Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)));

            promptTokens.Add(string.IsNullOrWhiteSpace(fallback) ? "[vector-context]" : fallback);
        }

        var promptText = string.Join(' ', promptTokens);

        return await _sql.ExecuteAsync(async (command, token) =>
        {
            command.CommandText = "dbo.sp_GenerateText";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@prompt", promptText));
            command.Parameters.Add(new SqlParameter("@max_tokens", maxTokens));
            command.Parameters.Add(new SqlParameter("@temperature", temperature));
            command.Parameters.Add(new SqlParameter("@ModelIds", DBNull.Value));
            command.Parameters.Add(new SqlParameter("@top_k", Math.Clamp(maxTokens, 1, 12)));

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            if (!await reader.ReadAsync(token).ConfigureAwait(false))
            {
                _logger.LogWarning("Stored procedure sp_GenerateText returned no rows");
                return new GenerationResult
                {
                    GeneratedText = string.Empty,
                    TokenIds = Array.Empty<int>(),
                    TokenConfidences = Array.Empty<float>(),
                    TokenCount = 0,
                    AverageConfidence = 0.0f,
                    InferenceId = 0
                };
            }

            var inferenceId = GetInt64(reader, "InferenceId");
            var generatedText = GetString(reader, "GeneratedText") ?? string.Empty;
            var tokenCount = GetNullableInt(reader, "TokensGenerated") ?? 0;
            var tokenDetailsJson = GetString(reader, "TokenDetails");

            var tokenDetails = new List<(int? TokenId, float Score)>();

            if (!string.IsNullOrWhiteSpace(tokenDetailsJson))
            {
                try
                {
                    using var json = JsonDocument.Parse(tokenDetailsJson);
                    foreach (var element in json.RootElement.EnumerateArray())
                    {
                        int? tokenId = null;
                        if (element.TryGetProperty("AtomId", out var atomIdProperty) &&
                            atomIdProperty.ValueKind == JsonValueKind.Number)
                        {
                            var atomId = atomIdProperty.GetInt64();
                            if (atomId <= int.MaxValue)
                            {
                                tokenId = (int)atomId;
                            }
                        }

                        float scoreValue = 0f;
                        if (element.TryGetProperty("Score", out var scoreProperty) &&
                            scoreProperty.ValueKind == JsonValueKind.Number &&
                            scoreProperty.TryGetDouble(out var scoreDouble))
                        {
                            scoreValue = (float)scoreDouble;
                        }
                        else if (element.TryGetProperty("Distance", out var distanceProperty) &&
                            distanceProperty.ValueKind == JsonValueKind.Number)
                        {
                            var distance = (float)distanceProperty.GetDouble();
                            scoreValue = distance == 0 ? 1.0f : 1.0f / (1.0f + distance);
                        }

                        tokenDetails.Add((tokenId, scoreValue));
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse token details JSON from sp_GenerateText");
                }
            }

            var tokenConfidencesAll = NormalizeScores(tokenDetails.Select(detail => detail.Score).ToList());

            var tokenIds = new List<int>();
            var tokenConfidences = new List<float>();
            for (var i = 0; i < tokenDetails.Count; i++)
            {
                var detail = tokenDetails[i];
                if (!detail.TokenId.HasValue)
                {
                    continue;
                }

                tokenIds.Add(detail.TokenId.Value);
                tokenConfidences.Add(i < tokenConfidencesAll.Count ? tokenConfidencesAll[i] : 0f);
            }

            var finalTokenCount = tokenCount > 0 ? tokenCount : tokenIds.Count;
            var averageConfidence = tokenConfidences.Count > 0
                ? tokenConfidences.Average()
                : 0f;

            return new GenerationResult
            {
                GeneratedText = generatedText,
                TokenIds = tokenIds,
                TokenConfidences = tokenConfidences,
                TokenCount = finalTokenCount,
                AverageConfidence = averageConfidence,
                InferenceId = inferenceId
            };
        }, cancellationToken).ConfigureAwait(false);
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

        if (atomEmbeddingIds.Count == 0)
        {
            return new SemanticFeatures();
        }

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

        var featureRows = await _sql.ExecuteAsync(async (command, token) =>
        {
            var parameterNames = new string[atomEmbeddingIds.Count];
            for (var i = 0; i < atomEmbeddingIds.Count; i++)
            {
                var parameterName = $"@id{i}";
                parameterNames[i] = parameterName;
                command.Parameters.Add(new SqlParameter(parameterName, atomEmbeddingIds[i]));
            }

            command.CommandText = $@"
SELECT
    sf.AtomEmbeddingId,
    sf.TopicTechnical,
    sf.TopicBusiness,
    sf.TopicScientific,
    sf.TopicCreative,
    sf.SentimentScore,
    sf.FormalityScore,
    sf.ComplexityScore,
    sf.TemporalRelevance,
    sf.TextLength,
    sf.WordCount,
    sf.AvgWordLength,
    sf.UniqueWordRatio,
    a.CanonicalText
FROM dbo.SemanticFeatures AS sf
INNER JOIN dbo.AtomEmbeddings AS ae ON ae.AtomEmbeddingId = sf.AtomEmbeddingId
INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
WHERE sf.AtomEmbeddingId IN ({string.Join(",", parameterNames)});";
            command.CommandType = CommandType.Text;

            var rows = new List<SemanticFeatureRow>();
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                rows.Add(new SemanticFeatureRow(
                    GetInt64(reader, "AtomEmbeddingId"),
                    GetNullableDouble(reader, "TopicTechnical") ?? 0.0,
                    GetNullableDouble(reader, "TopicBusiness") ?? 0.0,
                    GetNullableDouble(reader, "TopicScientific") ?? 0.0,
                    GetNullableDouble(reader, "TopicCreative") ?? 0.0,
                    GetNullableDouble(reader, "SentimentScore") ?? 0.0,
                    GetNullableDouble(reader, "FormalityScore") ?? 0.0,
                    GetNullableDouble(reader, "ComplexityScore") ?? 0.0,
                    GetNullableDouble(reader, "TemporalRelevance") ?? 0.0,
                    GetNullableInt(reader, "TextLength") ?? 0,
                    GetNullableInt(reader, "WordCount") ?? 0,
                    GetNullableDouble(reader, "AvgWordLength") ?? 0.0,
                    GetNullableDouble(reader, "UniqueWordRatio") ?? 0.0,
                    GetString(reader, "CanonicalText") ?? string.Empty));
            }

            return rows;
        }, cancellationToken).ConfigureAwait(false);

        if (featureRows.Count == 0)
        {
            _logger.LogWarning("Semantic feature rows were not returned for embeddings {Ids}", string.Join(',', atomEmbeddingIds));
            return new SemanticFeatures();
        }

        var aggregate = AggregateSemanticFeatures(featureRows);

        _logger.LogInformation("Semantic features computed for {Count} embeddings", featureRows.Count);
        return aggregate;
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
            command.Parameters.Add(new SqlParameter("@query_embedding", padded.ToSqlVector()));
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

    private static IReadOnlyList<float> NormalizeScores(IReadOnlyList<float> rawScores)
    {
        if (rawScores.Count == 0)
        {
            return Array.Empty<float>();
        }

        var maxScore = rawScores.Max();
        var expScores = new double[rawScores.Count];
        double sum = 0;
        for (var i = 0; i < rawScores.Count; i++)
        {
            var value = Math.Exp(rawScores[i] - maxScore);
            expScores[i] = value;
            sum += value;
        }

        if (sum <= double.Epsilon)
        {
            return rawScores.Select(_ => 0f).ToArray();
        }

        var confidences = new float[rawScores.Count];
        for (var i = 0; i < rawScores.Count; i++)
        {
            confidences[i] = (float)(expScores[i] / sum);
        }

        return confidences;
    }

    private static SemanticFeatures AggregateSemanticFeatures(IReadOnlyList<SemanticFeatureRow> rows)
    {
        var count = rows.Count;

        var topicScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["technical"] = rows.Sum(row => row.TopicTechnical) / count,
            ["business"] = rows.Sum(row => row.TopicBusiness) / count,
            ["scientific"] = rows.Sum(row => row.TopicScientific) / count,
            ["creative"] = rows.Sum(row => row.TopicCreative) / count
        };

        var sortedTopics = topicScores
            .OrderByDescending(pair => pair.Value)
            .Where(pair => pair.Value > 0.15)
            .Select(pair => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pair.Key))
            .ToList();

        if (sortedTopics.Count == 0)
        {
            sortedTopics.Add(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                topicScores.OrderByDescending(pair => pair.Value).First().Key));
        }

        var sentiment = rows.Average(row => row.SentimentScore);
        var temporalRelevance = rows.Average(row => row.TemporalRelevance);

        var keywordsFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var entityFrequency = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.CanonicalText))
            {
                continue;
            }

            foreach (Match match in WordRegex.Matches(row.CanonicalText))
            {
                var token = match.Value;
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                var lower = token.ToLowerInvariant();
                if (StopWords.Contains(lower))
                {
                    continue;
                }

                keywordsFrequency[lower] = keywordsFrequency.TryGetValue(lower, out var countValue)
                    ? countValue + 1
                    : 1;

                if (char.IsUpper(token[0]) && token.Any(char.IsLetter))
                {
                    entityFrequency[token] = entityFrequency.TryGetValue(token, out var entityCount)
                        ? entityCount + 1
                        : 1;
                }
            }
        }

        var keywords = keywordsFrequency
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(pair => pair.Key)
            .ToArray();

        var entities = entityFrequency.Count > 0
            ? entityFrequency
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(8)
                .Select(pair => pair.Key)
                .ToArray()
            : keywords
                .Take(5)
                .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word))
                .ToArray();

        var featureScores = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            ["TopicTechnical"] = (float)topicScores["technical"],
            ["TopicBusiness"] = (float)topicScores["business"],
            ["TopicScientific"] = (float)topicScores["scientific"],
            ["TopicCreative"] = (float)topicScores["creative"],
            ["FormalityScore"] = (float)(rows.Sum(row => row.FormalityScore) / count),
            ["ComplexityScore"] = (float)(rows.Sum(row => row.ComplexityScore) / count),
            ["TemporalRelevance"] = (float)temporalRelevance,
            ["AverageWordLength"] = (float)(rows.Sum(row => row.AverageWordLength) / count),
            ["UniqueWordRatio"] = (float)(rows.Sum(row => row.UniqueWordRatio) / count)
        };

        return new SemanticFeatures
        {
            Topics = sortedTopics,
            SentimentScore = (float)sentiment,
            Entities = entities,
            Keywords = keywords,
            TemporalRelevance = (float)temporalRelevance,
            FeatureScores = featureScores
        };
    }

    private static readonly Regex WordRegex = new("[A-Za-z0-9']+", RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(
        new[]
        {
            "the", "and", "or", "to", "a", "of", "in", "for", "on", "with", "is",
            "are", "was", "were", "be", "by", "as", "at", "it", "an", "this", "that",
            "from", "but", "not", "have", "has", "had", "will", "would", "can", "could", "should",
            "we", "you", "they", "their", "its", "our", "your", "i"
        },
        StringComparer.OrdinalIgnoreCase);

    private sealed record SemanticFeatureRow(
        long AtomEmbeddingId,
        double TopicTechnical,
        double TopicBusiness,
        double TopicScientific,
        double TopicCreative,
        double SentimentScore,
        double FormalityScore,
        double ComplexityScore,
        double TemporalRelevance,
        int TextLength,
        int WordCount,
        double AverageWordLength,
        double UniqueWordRatio,
        string CanonicalText);
}
