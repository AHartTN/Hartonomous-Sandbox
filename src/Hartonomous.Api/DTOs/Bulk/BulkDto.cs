using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Bulk;

public class BulkIngestRequest
{
    [Required]
    public required List<BulkContentItem> Items { get; set; }
    
    public int? ModelId { get; set; }
    
    public bool ProcessAsync { get; set; } = true;
    
    public string? CallbackUrl { get; set; }
    
    public Dictionary<string, string>? Metadata { get; set; }
}

public class BulkContentItem
{
    [Required]
    public required string Modality { get; set; }
    
    public string? CanonicalText { get; set; }
    
    public string? BinaryDataBase64 { get; set; }
    
    public string? ContentUrl { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

public class BulkIngestResponse
{
    public required string JobId { get; set; }
    public required string Status { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int FailedItems { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CallbackUrl { get; set; }
    public string? StatusUrl { get; set; }
}

public class BulkJobStatusResponse
{
    public required string JobId { get; set; }
    public required string Status { get; set; } // pending, processing, completed, failed, cancelled
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessItems { get; set; }
    public int FailedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? TotalDuration { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public required List<BulkJobItemResult> Results { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BulkJobItemResult
{
    public int ItemIndex { get; set; }
    public required string Status { get; set; }
    public long? AtomId { get; set; }
    public string? ContentHash { get; set; }
    public bool IsDuplicate { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? ProcessingTime { get; set; }
}

public class CancelBulkJobRequest
{
    [Required]
    public required string JobId { get; set; }
    
    public string? Reason { get; set; }
}

public class CancelBulkJobResponse
{
    public required string JobId { get; set; }
    public bool Success { get; set; }
    public required string Status { get; set; }
    public int ItemsProcessedBeforeCancellation { get; set; }
    public string? Message { get; set; }
}

public class BulkUploadRequest
{
    [Required]
    public required string Modality { get; set; }
    
    public int? ModelId { get; set; }
    
    public bool ExtractMetadata { get; set; } = true;
    
    public bool EnableDeduplication { get; set; } = true;
    
    public Dictionary<string, string>? GlobalMetadata { get; set; }
}

public class BulkUploadResponse
{
    public required string JobId { get; set; }
    public int FilesReceived { get; set; }
    public long TotalBytes { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ListBulkJobsRequest
{
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
    
    public int PageNumber { get; set; } = 1;
    
    public string? Status { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
}

public class ListBulkJobsResponse
{
    public required List<BulkJobSummary> Jobs { get; set; }
    public int TotalJobs { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class BulkJobSummary
{
    public required string JobId { get; set; }
    public required string Status { get; set; }
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
}

public class RetryFailedItemsRequest
{
    [Required]
    public required string JobId { get; set; }
    
    public bool OnlyRetryFailed { get; set; } = true;
}

public class RetryFailedItemsResponse
{
    public required string NewJobId { get; set; }
    public int ItemsToRetry { get; set; }
    public required string Status { get; set; }
}
