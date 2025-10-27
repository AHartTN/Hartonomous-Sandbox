using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a token in a model's vocabulary with its associated embedding vector.
/// </summary>
public class TokenVocabulary
{
    /// <summary>
    /// Gets or sets the unique identifier for the vocabulary entry.
    /// </summary>
    public long VocabId { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the associated model.
    /// </summary>
    public int ModelId { get; set; }
    
    /// <summary>
    /// Gets or sets the token text.
    /// </summary>
    public required string Token { get; set; }
    
    /// <summary>
    /// Gets or sets the numeric token identifier used by the model.
    /// </summary>
    public int TokenId { get; set; }
    
    /// <summary>
    /// Gets or sets the token type (e.g., 'word', 'subword', 'special').
    /// </summary>
    public string? TokenType { get; set; }
    
    /// <summary>
    /// Gets or sets the token embedding as a VECTOR (mapped to SQL Server 2025 VECTOR type).
    /// </summary>
    public SqlVector<float>? Embedding { get; set; }
    
    /// <summary>
    /// Gets or sets the dimensionality of the embedding vector.
    /// </summary>
    public int? EmbeddingDim { get; set; }
    
    /// <summary>
    /// Gets or sets the frequency count of this token in the training data or usage.
    /// </summary>
    public long Frequency { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets the timestamp of the last time this token was used in inference.
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// Gets or sets the associated model.
    /// </summary>
    public Model Model { get; set; } = null!;
}
