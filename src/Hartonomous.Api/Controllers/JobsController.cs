using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hartonomous.Api.Controllers;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Infrastructure.Jobs;
using Hartonomous.Infrastructure.Jobs.Processors;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Controller for managing and monitoring background jobs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class JobsController : ApiControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>
    /// Gets a specific job by ID.
    /// </summary>
    [HttpGet("{jobId:long}")]
    public async Task<IActionResult> GetJob(long jobId)
    {
        var job = await _jobService.GetJobAsync(jobId);

        if (job == null)
        {
            return NotFound(Failure<object>(
                new[] { ErrorDetailFactory.NotFound("Job", jobId.ToString()) }
            ));
        }

        return Ok(Success(job));
    }

    /// <summary>
    /// Gets jobs by status with pagination.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetJobsByStatus(
        [FromQuery] JobStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        if (take > 1000)
        {
            return BadRequest(Failure<object>(
                new[] { ErrorDetailFactory.Validation("take", "Maximum value is 1000") }
            ));
        }

        var jobs = status.HasValue
            ? await _jobService.GetJobsByStatusAsync(status.Value, skip, take)
            : new List<BackgroundJob>(); // Could add GetAllJobs method to service

        return Ok(Success(new
        {
            jobs,
            pagination = new { skip, take, count = jobs.Count }
        }));
    }

    /// <summary>
    /// Enqueues a cleanup job to remove old logs, cache, etc.
    /// </summary>
    [HttpPost("cleanup")]
    public async Task<IActionResult> EnqueueCleanupJob([FromBody] CleanupJobPayload payload)
    {
        var jobId = await _jobService.EnqueueAsync("Cleanup", payload, new JobEnqueueOptions
        {
            Priority = 5,
            MaxRetries = 2,
            CreatedBy = User.Identity?.Name
        });

        return Ok(Success(new { jobId, message = "Cleanup job enqueued successfully" }));
    }

    /// <summary>
    /// Enqueues an index maintenance job.
    /// </summary>
    [HttpPost("index-maintenance")]
    public async Task<IActionResult> EnqueueIndexMaintenanceJob([FromBody] IndexMaintenancePayload payload)
    {
        var jobId = await _jobService.EnqueueAsync("IndexMaintenance", payload, new JobEnqueueOptions
        {
            Priority = 7,
            MaxRetries = 1,
            CreatedBy = User.Identity?.Name
        });

        return Ok(Success(new { jobId, message = "Index maintenance job enqueued successfully" }));
    }

    /// <summary>
    /// Enqueues an analytics report generation job.
    /// </summary>
    [HttpPost("analytics")]
    public async Task<IActionResult> EnqueueAnalyticsJob([FromBody] AnalyticsJobPayload payload)
    {
        var jobId = await _jobService.EnqueueAsync("Analytics", payload, new JobEnqueueOptions
        {
            Priority = 3,
            MaxRetries = 2,
            CreatedBy = User.Identity?.Name,
            TenantId = payload.TenantId
        });

        return Ok(Success(new { jobId, message = "Analytics job enqueued successfully" }));
    }

    /// <summary>
    /// Cancels a pending or scheduled job.
    /// </summary>
    [HttpPost("{jobId:long}/cancel")]
    public async Task<IActionResult> CancelJob(long jobId)
    {
        var success = await _jobService.CancelJobAsync(jobId);

        if (!success)
        {
            return NotFound(Failure<object>(
                new[] { ErrorDetailFactory.NotFound("Job", jobId.ToString()) }
            ));
        }

        return Ok(Success(new { jobId, message = "Job cancelled successfully" }));
    }

    /// <summary>
    /// Gets job statistics and monitoring metrics.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetJobStatistics()
    {
        var stats = new
        {
            pending = (await _jobService.GetJobsByStatusAsync(JobStatus.Pending, 0, 1)).Count,
            inProgress = (await _jobService.GetJobsByStatusAsync(JobStatus.InProgress, 0, 1)).Count,
            completed = (await _jobService.GetJobsByStatusAsync(JobStatus.Completed, 0, 100)).Count,
            failed = (await _jobService.GetJobsByStatusAsync(JobStatus.Failed, 0, 100)).Count,
            deadLettered = (await _jobService.GetJobsByStatusAsync(JobStatus.DeadLettered, 0, 100)).Count,
            scheduled = (await _jobService.GetJobsByStatusAsync(JobStatus.Scheduled, 0, 100)).Count
        };

        return Ok(Success(stats));
    }

    /// <summary>
    /// Schedules a recurring cleanup job (daily).
    /// </summary>
    [HttpPost("schedule/cleanup")]
    public async Task<IActionResult> ScheduleDailyCleanup([FromBody] CleanupJobPayload payload)
    {
        // Schedule for tomorrow at 2 AM UTC
        var scheduledTime = DateTime.UtcNow.Date.AddDays(1).AddHours(2);

        var jobId = await _jobService.EnqueueAsync("Cleanup", payload, new JobEnqueueOptions
        {
            Priority = 5,
            MaxRetries = 2,
            ScheduledAtUtc = scheduledTime,
            CreatedBy = User.Identity?.Name
        });

        return Ok(Success(new
        {
            jobId,
            scheduledAt = scheduledTime,
            message = $"Cleanup job scheduled for {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC"
        }));
    }

    /// <summary>
    /// Schedules a recurring analytics job (weekly).
    /// </summary>
    [HttpPost("schedule/analytics")]
    public async Task<IActionResult> ScheduleWeeklyAnalytics([FromBody] AnalyticsJobPayload payload)
    {
        // Schedule for next Monday at 1 AM UTC
        var now = DateTime.UtcNow;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        var scheduledTime = now.Date.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday).AddHours(1);

        var jobId = await _jobService.EnqueueAsync("Analytics", payload, new JobEnqueueOptions
        {
            Priority = 3,
            MaxRetries = 2,
            ScheduledAtUtc = scheduledTime,
            CreatedBy = User.Identity?.Name,
            TenantId = payload.TenantId
        });

        return Ok(Success(new
        {
            jobId,
            scheduledAt = scheduledTime,
            message = $"Analytics job scheduled for {scheduledTime:yyyy-MM-dd HH:mm:ss} UTC"
        }));
    }
}
