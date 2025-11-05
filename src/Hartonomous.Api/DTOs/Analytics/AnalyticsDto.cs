using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Analytics;

public class UsageAnalyticsRequest
{
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    public string? Modality { get; set; }
    public string? GroupBy { get; set; } = "day"; // day, week, month
}

public class UsageAnalyticsResponse
{
    public required List<UsageDataPoint> DataPoints { get; set; }
    public UsageSummary Summary { get; set; } = new();
}

public class UsageDataPoint
{
    public DateTime Timestamp { get; set; }
    public long TotalRequests { get; set; }
    public long UniqueAtoms { get; set; }
    public long DeduplicatedCount { get; set; }
    public double DeduplicationRate { get; set; }
    public long TotalBytesProcessed { get; set; }
    public double AvgResponseTimeMs { get; set; }
}

public class UsageSummary
{
    public long TotalRequests { get; set; }
    public long TotalAtoms { get; set; }
    public long TotalDeduped { get; set; }
    public double OverallDeduplicationRate { get; set; }
    public long TotalBytesProcessed { get; set; }
    public double AvgResponseTimeMs { get; set; }
}

public class ModelPerformanceRequest
{
    public int? ModelId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ModelPerformanceResponse
{
    public required List<ModelPerformanceMetric> Metrics { get; set; }
}

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

public class EmbeddingStatsRequest
{
    public string? EmbeddingType { get; set; }
    public int? ModelId { get; set; }
}

public class EmbeddingStatsResponse
{
    public required List<EmbeddingTypeStat> Stats { get; set; }
    public EmbeddingOverallStats Overall { get; set; } = new();
}

public class EmbeddingTypeStat
{
    public required string EmbeddingType { get; set; }
    public int? ModelId { get; set; }
    public string? ModelName { get; set; }
    public long TotalEmbeddings { get; set; }
    public long UniqueAtoms { get; set; }
    public int? AvgDimension { get; set; }
    public long UsePaddingCount { get; set; }
    public long ComponentStorageCount { get; set; }
    public double AvgSpatialDistance { get; set; }
}

public class EmbeddingOverallStats
{
    public long TotalEmbeddings { get; set; }
    public long UniqueAtoms { get; set; }
    public int DistinctEmbeddingTypes { get; set; }
    public int DistinctModels { get; set; }
}

public class StorageMetricsResponse
{
    public long TotalAtoms { get; set; }
    public long TotalEmbeddings { get; set; }
    public long TotalTensorAtoms { get; set; }
    public long TotalModels { get; set; }
    public long TotalLayers { get; set; }
    public long TotalInferenceRequests { get; set; }
    public StorageSizeBreakdown SizeBreakdown { get; set; } = new();
    public DeduplicationMetrics Deduplication { get; set; } = new();
}

public class StorageSizeBreakdown
{
    public long AtomTableSizeMB { get; set; }
    public long EmbeddingTableSizeMB { get; set; }
    public long TensorAtomTableSizeMB { get; set; }
    public long FilestreamSizeMB { get; set; }
    public long TotalDatabaseSizeMB { get; set; }
}

public class DeduplicationMetrics
{
    public long TotalAtomReferences { get; set; }
    public long UniqueAtoms { get; set; }
    public double SpaceSavingsPercent { get; set; }
    public long EstimatedBytesSaved { get; set; }
}

public class TopAtomsRequest
{
    [Range(1, 1000)]
    public int TopK { get; set; } = 100;
    
    public string? Modality { get; set; }
    public string? OrderBy { get; set; } = "reference_count"; // reference_count, embedding_count, last_accessed
}

public class TopAtomsResponse
{
    public required List<AtomRankingEntry> Rankings { get; set; }
}

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
