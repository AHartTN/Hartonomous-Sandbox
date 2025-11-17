using Hartonomous.Data.Entities;
using Hartonomous.Core.Models;
using Hartonomous.Core.ValueObjects;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Service for orchestrating AI inference operations using T-SQL stored procedures.
/// Wraps database-native inference with high-level C# API.
/// </summary>
public interface IInferenceService
{
    /// <summary>
    /// Performs semantic search using exact VECTOR_DISTANCE calculation.
    /// Best for datasets &lt;50K embeddings. O(n) complexity.
    /// </summary>
    /// <param name="queryVector">Query embedding vector (768 dimensions)</param>
    /// <param name="topK">Number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of embeddings with similarity scores, sorted by distance ascending</returns>
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs approximate spatial search using GEOMETRY spatial indexes.
    /// Best for datasets &gt;50K embeddings. O(log n) complexity.
    /// </summary>
    /// <param name="queryVector">Query embedding vector (768 dimensions)</param>
    /// <param name="topK">Number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of embeddings with spatial distance scores</returns>
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> SpatialSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs hybrid search: spatial filter (100 candidates) + vector rerank (top K).
    /// Optimal balance: O(log n) + O(k). Recommended for production.
    /// </summary>
    /// <param name="queryVector">Query embedding vector (768 dimensions)</param>
    /// <param name="topK">Number of final results to return</param>
    /// <param name="candidateCount">Number of spatial candidates to filter (default 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of embeddings with exact similarity scores, spatial pre-filtered</returns>
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> HybridSearchAsync(
        float[] queryVector,
        int topK = 10,
        int candidateCount = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs ensemble inference: combines multiple models via weighted voting.
    /// Uses sp_EnsembleInference to execute inference across multiple models and aggregate results.
    /// </summary>
    /// <param name="inputData">Input data for inference (text, image bytes, etc.)</param>
    /// <param name="modelIds">List of model IDs to use in ensemble</param>
    /// <param name="weights">Optional weights for each model (defaults to uniform)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inference result with aggregated outputs and confidence scores</returns>
    Task<EnsembleInferenceResult> EnsembleInferenceAsync(
        string inputData,
        IReadOnlyList<int> modelIds,
        IReadOnlyList<float>? weights = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates text using spatial nearest neighbors instead of autoregressive decoding.
    /// Novel approach: finds semantically similar embeddings in spatial index, returns associated text.
    /// </summary>
    /// <param name="promptEmbedding">Embedding of the prompt/context</param>
    /// <param name="maxTokens">Maximum number of tokens to generate</param>
    /// <param name="temperature">Sampling temperature for diversity (0.0-1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated text with token IDs and confidence scores</returns>
    Task<GenerationResult> GenerateViaSpatialAsync(
        float[] promptEmbedding,
        int maxTokens = 50,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes semantic features (topics, sentiment, entities) from embeddings.
    /// Uses clustering and spatial analysis to extract high-level features.
    /// </summary>
    /// <param name="embeddingIds">List of embedding IDs to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Semantic features with confidence scores</returns>
    Task<ValueObjects.SemanticFeatures> ComputeSemanticFeaturesAsync(
        IReadOnlyList<long> atomEmbeddingIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs semantic search with filters (topic, sentiment, temporal relevance).
    /// Combines VECTOR_DISTANCE with WHERE clause filtering.
    /// </summary>
    /// <param name="queryVector">Query embedding vector</param>
    /// <param name="topK">Number of results</param>
    /// <param name="topicFilter">Optional topic filter</param>
    /// <param name="minSentiment">Optional minimum sentiment score</param>
    /// <param name="maxAge">Optional maximum age in days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered search results</returns>
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticFilteredSearchAsync(
        float[] queryVector,
        int topK = 10,
        string? topicFilter = null,
        float? minSentiment = null,
        int? maxAge = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits user feedback for an inference request.
    /// Updates InferenceRequests table with UserRating and UserFeedback.
    /// Feedback is used by sp_UpdateModelWeightsFromFeedback for learning.
    /// </summary>
    /// <param name="inferenceId">Inference request ID</param>
    /// <param name="rating">User rating (1-5 scale)</param>
    /// <param name="feedback">Optional text feedback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SubmitFeedbackAsync(
        long inferenceId,
        byte rating,
        string? feedback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes model weight updates from accumulated user feedback.
    /// Calls sp_UpdateModelWeightsFromFeedback with specified learning rate.
    /// </summary>
    /// <param name="learningRate">Learning rate for weight updates (default 0.001)</param>
    /// <param name="minRatings">Minimum number of ratings required before updating (default 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of layers updated</returns>
    Task<int> UpdateWeightsFromFeedbackAsync(
        float learningRate = 0.001f,
        int minRatings = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a specific model for inference.
    /// </summary>
    /// <param name="modelId">The ID of the model to invoke.</param>
    /// <param name="prompt">The input prompt for inference.</param>
    /// <param name="context">Additional context for the model.</param>
    /// <param name="parameters">Optional model-specific parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model output as a string.</returns>
    Task<string> InvokeModelAsync(
        int modelId,
        string prompt,
        string? context,
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken = default);
}
