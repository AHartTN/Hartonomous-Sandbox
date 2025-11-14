using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities;

public partial class TextDocument : ITextDocument
{
    public long DocId { get; set; }

    public string? SourcePath { get; set; }

    public string? SourceUrl { get; set; }

    public string RawText { get; set; } = null!;

    public string? Language { get; set; }

    public int? CharCount { get; set; }

    public int? WordCount { get; set; }

    public SqlVector<float>? GlobalEmbedding { get; set; }

    public SqlVector<float>? TopicVector { get; set; }

    public float? SentimentScore { get; set; }

    public float? Toxicity { get; set; }

    public string? Metadata { get; set; }

    public DateTime? IngestionDate { get; set; }

    public DateTime? LastAccessed { get; set; }

    public long AccessCount { get; set; }
}
