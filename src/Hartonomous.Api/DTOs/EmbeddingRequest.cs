using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs;

/// <summary>
/// Request DTO for generating text embeddings.
/// </summary>
public class EmbeddingRequest
{
    /// <summary>
    /// Gets or sets the input text to embed.
    /// </summary>
    [Required]
    public required string Text { get; set; }
    
    /// <summary>
    /// Gets or sets the optional model ID to use for embedding generation.
    /// If not specified, the default text embedding model will be used.
    /// </summary>
    public int? ModelId { get; set; }
    
    /// <summary>
    /// Gets or sets the embedding type (e.g., "text", "multimodal").
    /// If not specified, defaults to standard text embedding.
    /// </summary>
    public string? EmbeddingType { get; set; }
}
