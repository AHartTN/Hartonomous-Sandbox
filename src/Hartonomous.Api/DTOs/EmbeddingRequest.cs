namespace Hartonomous.Api.DTOs;

/// <summary>
/// Request model for creating embeddings from text input.
/// </summary>
public record EmbeddingRequest(
    /// <summary>
    /// Text content to embed.
    /// </summary>
    string Text,

    /// <summary>
    /// Embedding model type (e.g., "text-embedding-3-large").
    /// </summary>
    string EmbeddingType = "text-embedding-3-large",

    /// <summary>
    /// Optional model ID to associate with the embedding.
    /// </summary>
    int? ModelId = null
);
