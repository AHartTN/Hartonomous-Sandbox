using System;

namespace Hartonomous.Api.DTOs.Analytics
{
    public class AtomRankingEntry
    {
        public long AtomId { get; set; }
        public required string Modality { get; set; }
        public string? CanonicalText { get; set; }
        public long ReferenceCount { get; set; }
        public int EmbeddingCount { get; set; }
        public DateTime? LastAccessed { get; set; }
        public double? AvgImportanceScore { get; set; }
    }
}
