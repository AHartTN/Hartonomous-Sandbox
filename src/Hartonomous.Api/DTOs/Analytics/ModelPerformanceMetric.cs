using System;

namespace Hartonomous.Api.DTOs.Analytics
{
    public class ModelPerformanceMetric
    {
        public int ModelId { get; set; }
        public required string ModelName { get; set; }
        public long TotalInferences { get; set; }
        public double AvgInferenceTimeMs { get; set; }
        public double AvgConfidenceScore { get; set; }
        public double CacheHitRate { get; set; }
        public long TotalTokensGenerated { get; set; }
        public DateTime? LastUsed { get; set; }
        public int? UsageCount { get; set; }
    }
}
