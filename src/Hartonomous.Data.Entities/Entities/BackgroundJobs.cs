using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

/// <summary>
/// Background job queue for asynchronous task processing with priority-based execution and retry logic.
/// </summary>
public partial class BackgroundJobs : IBackgroundJobs
{
    public long JobId { get; set; }

    public string JobType { get; set; } = null!;

    public string? Payload { get; set; }

    public int Status { get; set; }

    public int AttemptCount { get; set; }

    public int MaxRetries { get; set; }

    public int Priority { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ScheduledAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? ResultData { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ErrorStackTrace { get; set; }

    public int? TenantId { get; set; }

    public string? CreatedBy { get; set; }

    public string? CorrelationId { get; set; }
}
