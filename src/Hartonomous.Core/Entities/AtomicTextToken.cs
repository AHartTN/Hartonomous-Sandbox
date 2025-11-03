using Microsoft.Data.SqlTypes;
using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a unique atomic text token with content-addressable deduplication.
/// Stores token text and optional embedding for semantic deduplication.
/// </summary>
public class AtomicTextToken : IReferenceTrackedEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the token.
    /// </summary>
    public long TokenId { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the token text - serves as primary key.
    /// </summary>
    public byte[] TokenHash { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the text content of the token.
    /// </summary>
    public required string TokenText { get; set; }

    /// <summary>
    /// Gets or sets the length of the token text.
    /// </summary>
    public int TokenLength { get; set; }

    /// <summary>
    /// Gets or sets the token embedding vector (VECTOR type).
    /// </summary>
    public SqlVector<float>? TokenEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the name of the embedding model used.
    /// </summary>
    public string? EmbeddingModel { get; set; }

    /// <summary>
    /// Gets or sets the optional vocabulary ID if token is in model vocabulary.
    /// </summary>
    public int? VocabId { get; set; }

    /// <summary>
    /// Gets or sets the number of times this token is referenced.
    /// </summary>
    public long ReferenceCount { get; set; }

    /// <summary>
    /// Gets or sets when this token was first seen.
    /// </summary>
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this token was last referenced.
    /// </summary>
    public DateTime LastReferenced { get; set; } = DateTime.UtcNow;
}