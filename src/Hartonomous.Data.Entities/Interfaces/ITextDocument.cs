using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities;

public interface ITextDocument
{
    long DocId { get; set; }
    string? SourcePath { get; set; }
    string? SourceUrl { get; set; }
    string RawText { get; set; }
    string? Language { get; set; }
    int? CharCount { get; set; }
    int? WordCount { get; set; }
    SqlVector<float>? GlobalEmbedding { get; set; }
    SqlVector<float>? TopicVector { get; set; }
    float? SentimentScore { get; set; }
    float? Toxicity { get; set; }
    string? Metadata { get; set; }
    DateTime? IngestionDate { get; set; }
    DateTime? LastAccessed { get; set; }
    long AccessCount { get; set; }
}
