using Hartonomous.Core.Interfaces;
using Hartonomous.Core.ValueObjects;
using Hartonomous.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Orchestrates AI inference operations by calling T-SQL stored procedures.
/// Database-native AI: inference executes as SELECT statements in SQL Server.
/// </summary>
public sealed class InferenceOrchestrator : IInferenceService
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<InferenceOrchestrator> _logger;

    public InferenceOrchestrator(
        HartonomousDbContext context,
        ILogger<InferenceOrchestrator> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<EmbeddingSearchResult>> SemanticSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing semantic search with topK={TopK}", topK);

        var vectorParam = new SqlParameter("@queryVector", SqlDbType.Udt)
        {
            UdtTypeName = "VECTOR",
            Value = new SqlVector<float>(queryVector)
        };
        var topKParam = new SqlParameter("@topK", topK);

        var results = await _context.Embeddings
            .FromSqlRaw(@"
                EXEC dbo.sp_SemanticSearch 
                    @queryVector = @queryVector, 
                    @topK = @topK",
                vectorParam,
                topKParam)
            .Select(e => new EmbeddingSearchResult
            {
                EmbeddingId = e.EmbeddingId,
                SourceText = e.SourceText ?? string.Empty,
                SimilarityScore = 1.0f, // Placeholder - would come from procedure output
                Distance = 0.0f,
                CreatedTimestamp = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Semantic search returned {Count} results", results.Count);
        return results;
    }

    public async Task<IReadOnlyList<EmbeddingSearchResult>> SpatialSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing spatial search with topK={TopK}", topK);

        var vectorParam = new SqlParameter("@queryVector", SqlDbType.Udt)
        {
            UdtTypeName = "VECTOR",
            Value = new SqlVector<float>(queryVector)
        };
        var topKParam = new SqlParameter("@topK", topK);

        var results = await _context.Embeddings
            .FromSqlRaw(@"
                EXEC dbo.sp_ApproxSpatialSearch 
                    @queryVector = @queryVector, 
                    @topK = @topK",
                vectorParam,
                topKParam)
            .Select(e => new EmbeddingSearchResult
            {
                EmbeddingId = e.EmbeddingId,
                SourceText = e.SourceText ?? string.Empty,
                SimilarityScore = 0.0f,
                Distance = 0.0f,
                CreatedTimestamp = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Spatial search returned {Count} results", results.Count);
        return results;
    }

    public async Task<IReadOnlyList<EmbeddingSearchResult>> HybridSearchAsync(
        float[] queryVector,
        int topK = 10,
        int candidateCount = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing hybrid search with topK={TopK}, candidateCount={CandidateCount}",
            topK,
            candidateCount);

        var vectorParam = new SqlParameter("@queryVector", SqlDbType.Udt)
        {
            UdtTypeName = "VECTOR",
            Value = new SqlVector<float>(queryVector)
        };
        var topKParam = new SqlParameter("@topK", topK);
        var candidateParam = new SqlParameter("@candidateCount", candidateCount);

        var results = await _context.Embeddings
            .FromSqlRaw(@"
                EXEC dbo.sp_HybridSearch 
                    @queryVector = @queryVector, 
                    @topK = @topK,
                    @candidateCount = @candidateCount",
                vectorParam,
                topKParam,
                candidateParam)
            .Select(e => new EmbeddingSearchResult
            {
                EmbeddingId = e.EmbeddingId,
                SourceText = e.SourceText ?? string.Empty,
                SimilarityScore = 0.0f,
                Distance = 0.0f,
                CreatedTimestamp = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

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

        command.Parameters.Add(new SqlParameter("@promptEmbedding", SqlDbType.Udt)
        {
            UdtTypeName = "VECTOR",
            Value = new SqlVector<float>(promptEmbedding)
        });
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
        IReadOnlyList<long> embeddingIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Computing semantic features for {Count} embeddings",
            embeddingIds.Count);

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_ComputeSemanticFeatures";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@embeddingIds", string.Join(",", embeddingIds)));

        await command.ExecuteNonQueryAsync(cancellationToken);

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

    public async Task<IReadOnlyList<EmbeddingSearchResult>> SemanticFilteredSearchAsync(
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

        var vectorParam = new SqlParameter("@queryVector", SqlDbType.Udt)
        {
            UdtTypeName = "VECTOR",
            Value = new SqlVector<float>(queryVector)
        };
        var topKParam = new SqlParameter("@topK", topK);
        var topicParam = new SqlParameter("@topicFilter", (object?)topicFilter ?? DBNull.Value);
        var sentimentParam = new SqlParameter("@minSentiment", (object?)minSentiment ?? DBNull.Value);
        var ageParam = new SqlParameter("@maxAge", (object?)maxAge ?? DBNull.Value);

        var results = await _context.Embeddings
            .FromSqlRaw(@"
                EXEC dbo.sp_SemanticFilteredSearch 
                    @queryVector = @queryVector, 
                    @topK = @topK,
                    @topicFilter = @topicFilter,
                    @minSentiment = @minSentiment,
                    @maxAge = @maxAge",
                vectorParam,
                topKParam,
                topicParam,
                sentimentParam,
                ageParam)
            .Select(e => new EmbeddingSearchResult
            {
                EmbeddingId = e.EmbeddingId,
                SourceText = e.SourceText ?? string.Empty,
                SimilarityScore = 0.0f,
                Distance = 0.0f,
                CreatedTimestamp = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Filtered search returned {Count} results", results.Count);
        return results;
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
}
