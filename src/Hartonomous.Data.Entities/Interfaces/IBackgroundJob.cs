using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

/// <summary>
/// Background job queue for asynchronous task processing with priority-based execution and retry logic.
/// </summary>
public interface IBackgroundJob
{
    long JobId { get; set; }
    string JobType { get; set; }
    string? Payload { get; set; }
    int Status { get; set; }
    int AttemptCount { get; set; }
    int MaxRetries { get; set; }
    int Priority { get; set; }
    DateTime CreatedAtUtc { get; set; }
    DateTime? ScheduledAtUtc { get; set; }
    DateTime? StartedAtUtc { get; set; }
    DateTime? CompletedAtUtc { get; set; }
    string? ResultData { get; set; }
    string? ErrorMessage { get; set; }
    string? ErrorStackTrace { get; set; }
    int? TenantId { get; set; }
    string? CreatedBy { get; set; }
    string? CorrelationId { get; set; }
}
