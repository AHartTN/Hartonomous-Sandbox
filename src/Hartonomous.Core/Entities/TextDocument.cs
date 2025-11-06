using Microsoft.Data.SqlTypes;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a text document with embeddings and semantic features for NLP tasks.
/// Stores raw text along with computed semantic representations and metadata.
/// Maps to dbo.TextDocuments table from 02_MultiModalData.sql.
/// </summary>
public sealed class TextDocument
{
    /// <summary>
    /// Gets or sets the unique identifier for the text document.
    /// </summary>
    public long DocId { get; set; }

    /// <summary>
    /// Gets or sets the file system path where the document is stored.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Gets or sets the URL from which the document was retrieved.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Gets or sets the raw text content of the document.
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language code of the document (e.g., 'en', 'es', 'fr', 'de').
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the total number of characters in the document.
    /// </summary>
    public int? CharCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of words in the document.
    /// </summary>
    public int? WordCount { get; set; }

    /// <summary>
    /// Gets or sets the global document embedding vector (typically from BERT, sentence transformers, or similar).
    /// </summary>
    public SqlVector<float>? GlobalEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the dimensionality of the global embedding vector.
    /// </summary>
    public int? GlobalEmbeddingDim { get; set; }

    /// <summary>
    /// Gets or sets the topic distribution vector for topic modeling.
    /// </summary>
    public SqlVector<float>? TopicVector { get; set; }

    /// <summary>
    /// Gets or sets the sentiment score (-1.0 to 1.0, where negative is negative sentiment and positive is positive).
    /// </summary>
    public float? SentimentScore { get; set; }

    /// <summary>
    /// Gets or sets the toxicity score (0.0 to 1.0, where 0 is non-toxic and 1 is highly toxic).
    /// </summary>
    public float? Toxicity { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., named entities, keywords, author, publication date).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the document was ingested into the system.
    /// </summary>
    public DateTime? IngestionDate { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last access to this document.
    /// </summary>
    public DateTime? LastAccessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of times this document has been accessed.
    /// </summary>
    public long AccessCount { get; set; }
}
