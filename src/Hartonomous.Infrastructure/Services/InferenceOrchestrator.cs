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
    private readonly HartonomousDbContext _context;
    private readonly IAtomEmbeddingRepository _atomEmbeddings;
    private readonly ISqlCommandExecutor _sql;
    private readonly ILogger<InferenceOrchestrator> _logger;

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
                command.Parameters.Add(new SqlParameter("@atom_embedding_id", atomEmbeddingId));

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

    private static long GetInt64(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetInt64(ordinal) : 0;
    }

    private static string? GetString(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetString(ordinal) : null;
    }

    private static int? GetNullableInt(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetInt32(ordinal) : null;
    }

    private static double? GetNullableDouble(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetDouble(ordinal) : null;
    }

    private static DateTime? GetNullableDateTime(IDataRecord record, string columnName)
    {
        var ordinal = GetOrdinal(record, columnName);
        return ordinal >= 0 && !record.IsDBNull(ordinal) ? record.GetDateTime(ordinal) : null;
    }

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
