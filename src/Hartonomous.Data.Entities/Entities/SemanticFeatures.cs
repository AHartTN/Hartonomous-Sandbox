using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class SemanticFeatures : ISemanticFeatures
{
    public long AtomEmbeddingId { get; set; }

    public double? TopicTechnical { get; set; }

    public double? TopicBusiness { get; set; }

    public double? TopicScientific { get; set; }

    public double? TopicCreative { get; set; }

    public double? SentimentScore { get; set; }

    public double? FormalityScore { get; set; }

    public double? ComplexityScore { get; set; }

    public double? TemporalRelevance { get; set; }

    public DateTime? ReferenceDate { get; set; }

    public int? TextLength { get; set; }

    public int? WordCount { get; set; }

    public double? UniqueWordRatio { get; set; }

    public double? AvgWordLength { get; set; }

    public DateTime? ComputedAt { get; set; }

    public virtual AtomEmbeddings AtomEmbedding { get; set; } = null!;
}
