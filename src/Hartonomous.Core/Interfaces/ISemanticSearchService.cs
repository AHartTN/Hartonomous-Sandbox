using Hartonomous.Core.Models;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Provides semantic vector search capabilities using SQL Server stored procedures.
/// Supports both exact vector search and filtered search with topic/sentiment criteria.
/// </summary>
public interface ISemanticSearchService
{
    /// <summary>
    /// Executes an exact semantic vector search using cosine similarity.
    /// </summary>
    /// <param name="queryVector">Vector embedding representing the search query.</param>
    /// <param name="topK">Number of top results to return.</param>
    /// <param name="cancellationToken">Token to cancel command execution.</param>
    /// <returns>Collection of matched embeddings ordered by similarity.</returns>
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes semantic search with optional topic, sentiment, and recency filters.
    /// </summary>
    /// <param name="queryVector">Embedding for the query.</param>
    /// <param name="topK">Maximum results to return.</param>
    /// <param name="topicFilter">Optional topic constraint (e.g., "technical", "business").</param>
    /// <param name="minSentiment">Optional minimum sentiment threshold.</param>
    /// <param name="maxAge">Optional recency window for results in days.</param>
    /// <param name="cancellationToken">Token for cancelling the search.</param>
    /// <returns>Filtered list of embeddings meeting the criteria.</returns>
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> SemanticFilteredSearchAsync(
        float[] queryVector,
        int topK = 10,
        string? topicFilter = null,
        float? minSentiment = null,
        int? maxAge = null,
        CancellationToken cancellationToken = default);
}
