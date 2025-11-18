namespace Hartonomous.Infrastructure.Jobs;

/// <summary>
/// Background job status values matching dbo.BackgroundJobs.Status column.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is queued and waiting to execute.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Job is currently executing.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed but may be retried.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job exhausted retries and moved to dead letter queue.
    /// </summary>
    DeadLettered = 4,

    /// <summary>
    /// Job was cancelled before completion.
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Job is scheduled for future execution.
    /// </summary>
    Scheduled = 6
}
