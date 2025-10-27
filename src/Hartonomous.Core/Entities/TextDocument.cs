using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Primary text storage with embeddings and semantic features.
/// Maps to dbo.TextDocuments table from 02_MultiModalData.sql.
/// </summary>
public sealed class TextDocument
{
    public long DocId { get; set; }
    public string? SourcePath { get; set; }
    public string? SourceUrl { get; set; }

    // Text content
    public string RawText { get; set; } = string.Empty;
    public string? Language { get; set; } // 'en', 'es', 'fr'
    public int? CharCount { get; set; }
    public int? WordCount { get; set; }

    // Vector representations
    public SqlVector<float>? GlobalEmbedding { get; set; } // VECTOR(768)
    public int? GlobalEmbeddingDim { get; set; }

    // Semantic features
    public SqlVector<float>? TopicVector { get; set; } // VECTOR(100)
    public float? SentimentScore { get; set; }
    public float? Toxicity { get; set; }

    // Metadata
    public string? Metadata { get; set; } // JSON

    public DateTime? IngestionDate { get; set; }
    public DateTime? LastAccessed { get; set; }
    public long AccessCount { get; set; }
}
