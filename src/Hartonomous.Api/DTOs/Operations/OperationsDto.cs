using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Operations;

public class HealthCheckResponse
{
    public required string Status { get; set; } // healthy, degraded, unhealthy
    public required Dictionary<string, ComponentHealth> Components { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalCheckDuration { get; set; }
}

public class ComponentHealth
{
    public required string Status { get; set; }
    public string? Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

public class IndexMaintenanceRequest
{
    public string? IndexName { get; set; }
    public string? TableName { get; set; }
    public string Operation { get; set; } = "rebuild"; // rebuild, reorganize, update_statistics
    public int? FillFactor { get; set; }
    public bool Online { get; set; } = true;
}

public class IndexMaintenanceResponse
{
    public required List<IndexOperationResult> Results { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public class IndexOperationResult
{
    public required string IndexName { get; set; }
    public required string TableName { get; set; }
    public required string Operation { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TimeSpan Duration { get; set; }
    public double? FragmentationBefore { get; set; }
    public double? FragmentationAfter { get; set; }
}

public class CacheManagementRequest
{
    public string Operation { get; set; } = "clear"; // clear, warm, stats
    public string? CacheType { get; set; } // vector, model, embedding, all
    public int? ModelId { get; set; }
}

public class CacheManagementResponse
{
    public required string Operation { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public CacheStats? Stats { get; set; }
}

public class CacheStats
{
    public long TotalEntries { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRate { get; set; }
    public long MemoryUsedMB { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
}

public class DiagnosticRequest
{
    public string DiagnosticType { get; set; } = "slow_queries"; // slow_queries, blocking, deadlocks, resource_usage
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    [Range(1, 1000)]
    public int TopK { get; set; } = 10;
}

public class DiagnosticResponse
{
    public required string DiagnosticType { get; set; }
    public required List<DiagnosticEntry> Entries { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class DiagnosticEntry
{
    public required string Category { get; set; }
    public required string Description { get; set; }
    public DateTime? Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Query { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
}

public class ConfigurationRequest
{
    public required string Key { get; set; }
    public string? Value { get; set; }
}

public class ConfigurationResponse
{
    public required Dictionary<string, string> Settings { get; set; }
}

public class BackupRequest
{
    public string BackupType { get; set; } = "full"; // full, differential, log
    public string? BackupPath { get; set; }
    public bool Compression { get; set; } = true;
    public bool Checksum { get; set; } = true;
}

public class BackupResponse
{
    public required string BackupType { get; set; }
    public required string BackupPath { get; set; }
    public long BackupSizeMB { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class QueryStoreStatsResponse
{
    public bool QueryStoreEnabled { get; set; }
    public required string OperationMode { get; set; }
    public long TotalQueries { get; set; }
    public long TotalPlans { get; set; }
    public long CurrentStorageMB { get; set; }
    public long MaxStorageMB { get; set; }
    public double StorageUsedPercent { get; set; }
    public required List<TopQueryEntry> TopQueries { get; set; }
}

public class TopQueryEntry
{
    public long QueryId { get; set; }
    public required string QueryText { get; set; }
    public long ExecutionCount { get; set; }
    public double AvgDurationMs { get; set; }
    public double AvgCpuTimeMs { get; set; }
    public double AvgLogicalReads { get; set; }
    public DateTime? LastExecutionTime { get; set; }
}
