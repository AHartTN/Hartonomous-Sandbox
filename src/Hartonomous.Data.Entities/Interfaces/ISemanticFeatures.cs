using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ISemanticFeatures
{
    long AtomEmbeddingId { get; set; }
    double? TopicTechnical { get; set; }
    double? TopicBusiness { get; set; }
    double? TopicScientific { get; set; }
    double? TopicCreative { get; set; }
    double? SentimentScore { get; set; }
    double? FormalityScore { get; set; }
    double? ComplexityScore { get; set; }
    double? TemporalRelevance { get; set; }
    DateTime? ReferenceDate { get; set; }
    int? TextLength { get; set; }
    int? WordCount { get; set; }
    double? UniqueWordRatio { get; set; }
    double? AvgWordLength { get; set; }
    DateTime? ComputedAt { get; set; }
    AtomEmbeddings AtomEmbedding { get; set; }
}
