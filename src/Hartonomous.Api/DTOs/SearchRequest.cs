using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs;

/// <summary>
/// Request DTO for hybrid semantic search combining text queries, embeddings, and filters.
/// Supports both text-based and vector-based search with optional spatial and metadata filtering.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Gets or sets the text query to search for.
    /// Will be embedded automatically if provided without QueryEmbedding.
    /// </summary>
    public string? QueryText { get; set; }
    
    /// <summary>
    /// Gets or sets the pre-computed query embedding vector.
    /// Alternative to providing QueryText when embedding is already available.
    /// </summary>
    public float[]? QueryEmbedding { get; set; }
    
    /// <summary>
    /// Gets or sets the query vector (alias for QueryEmbedding for backward compatibility).
    /// </summary>
    public float[]? QueryVector { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// Must be between 1 and 1000. Defaults to 10.
    /// </summary>
    [Range(1, 1000)]
    public int TopK { get; set; } = 10;
    
    /// <summary>
    /// Gets or sets the modality filter (e.g., "text", "image", "audio", "video").
    /// Restricts results to specific content types.
    /// </summary>
    public string? ModalityFilter { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum cosine similarity threshold (0.0 to 1.0).
    /// Results with lower similarity scores will be excluded.
    /// </summary>
    public double? MinimumSimilarity { get; set; }
    
    /// <summary>
    /// Gets or sets the topic filter for domain-specific search.
    /// </summary>
    public string? TopicFilter { get; set; }
    
    /// <summary>
    /// Gets or sets the minimum sentiment score filter (-1.0 to 1.0).
    /// </summary>
    public float? MinSentiment { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum age in days for temporal filtering.
    /// Excludes content older than this threshold.
    /// </summary>
    public int? MaxAge { get; set; }
    
    /// <summary>
    /// Gets or sets the number of spatial candidates to retrieve before reranking.
    /// Must be between 10 and 10000. Defaults to 100.
    /// Higher values improve recall but increase latency.
    /// </summary>
    [Range(10, 10000)]
    public int CandidateCount { get; set; } = 100;
}
