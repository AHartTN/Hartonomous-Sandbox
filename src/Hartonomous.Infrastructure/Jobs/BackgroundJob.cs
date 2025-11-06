namespace Hartonomous.Infrastructure.Jobs;

/// <summary>
/// Enumeration of possible job states in the background processing pipeline.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is queued and waiting to be processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently being processed by a worker.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed and retry attempts remain.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job failed permanently after all retry attempts exhausted.
    /// </summary>
    DeadLettered = 4,

    /// <summary>
    /// Job was cancelled by user or system.
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Job is scheduled for future execution.
    /// </summary>
    Scheduled = 6
}

/// <summary>
/// Base class for background job data stored in database.
/// </summary>
public class BackgroundJob
{
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public long JobId { get; set; }

    /// <summary>
    /// Job type identifier (e.g., "EmbeddingGeneration", "ModelTraining", "DataSync").
    /// </summary>
    public required string JobType { get; set; }

    /// <summary>
    /// Serialized JSON payload containing job parameters.
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Current status of the job.
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>
    /// Number of times this job has been attempted.
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Maximum number of retry attempts before dead lettering.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Priority level (higher = more important). Default: 0.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// When the job was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the job should be executed (UTC). Null = execute immediately.
    /// </summary>
    public DateTime? ScheduledAtUtc { get; set; }

    /// <summary>
    /// When the job started processing (UTC). Null if not started.
    /// </summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// When the job completed/failed/cancelled (UTC). Null if still in progress.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Serialized JSON result data from successful execution.
    /// </summary>
    public string? ResultData { get; set; }

    /// <summary>
    /// Error message from last failed attempt.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Full exception stack trace from last failed attempt.
    /// </summary>
    public string? ErrorStackTrace { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant isolation.
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// User or service principal that created the job.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }
}
