using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Hartonomous.Data;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;

namespace Hartonomous.Infrastructure.Jobs;

/// <summary>
/// Service for enqueueing and managing background jobs.
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Enqueues a new background job.
    /// </summary>
    /// <typeparam name="TPayload">Payload type.</typeparam>
    /// <param name="jobType">Job type identifier.</param>
    /// <param name="payload">Job payload.</param>
    /// <param name="options">Optional job configuration.</param>
    /// <returns>Created job ID.</returns>
    Task<long> EnqueueAsync<TPayload>(string jobType, TPayload payload, JobEnqueueOptions? options = null)
        where TPayload : class;

    /// <summary>
    /// Gets job status by ID.
    /// </summary>
    Task<BackgroundJob?> GetJobAsync(long jobId);

    /// <summary>
    /// Gets jobs by status with pagination.
    /// </summary>
    Task<List<BackgroundJob>> GetJobsByStatusAsync(JobStatus status, int skip = 0, int take = 100);

    /// <summary>
    /// Cancels a pending/scheduled job.
    /// </summary>
    Task<bool> CancelJobAsync(long jobId);
}

/// <summary>
/// Options for enqueueing background jobs.
/// </summary>
public class JobEnqueueOptions
{
    /// <summary>
    /// Priority level (higher = more important). Default: 0.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Maximum retry attempts. Default: 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// When to execute the job (null = immediately).
    /// </summary>
    public DateTime? ScheduledAtUtc { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant isolation.
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// User/service principal that created the job.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Default implementation of IJobService.
/// </summary>
public class JobService : IJobService
{
    private readonly HartonomousDbContext _context;

    public JobService(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<long> EnqueueAsync<TPayload>(
        string jobType,
        TPayload payload,
        JobEnqueueOptions? options = null)
        where TPayload : class
    {
        options ??= new JobEnqueueOptions();

        var job = new BackgroundJob
        {
            JobType = jobType,
            Payload = JsonSerializer.Serialize(payload),
            Status = (int)(options.ScheduledAtUtc.HasValue ? JobStatus.Scheduled : JobStatus.Pending),
            Priority = options.Priority,
            MaxRetries = options.MaxRetries,
            ScheduledAtUtc = options.ScheduledAtUtc,
            TenantId = options.TenantId,
            CreatedBy = options.CreatedBy,
            CorrelationId = options.CorrelationId ?? Guid.NewGuid().ToString()
        };

        _context.BackgroundJobs.Add(job);
        await _context.SaveChangesAsync();

        return job.JobId;
    }

    public async Task<BackgroundJob?> GetJobAsync(long jobId)
    {
        return await _context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.JobId == jobId);
    }

    public async Task<List<BackgroundJob>> GetJobsByStatusAsync(JobStatus status, int skip = 0, int take = 100)
    {
        return await _context.BackgroundJobs
            .Where(j => j.Status == (int)status)
            .OrderByDescending(j => j.Priority)
            .ThenBy(j => j.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> CancelJobAsync(long jobId)
    {
        var job = await _context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.JobId == jobId);

        if (job == null)
        {
            return false;
        }

        // Can only cancel pending/scheduled jobs
        if (job.Status != (int)JobStatus.Pending && job.Status != (int)JobStatus.Scheduled)
        {
            return false;
        }

        job.Status = (int)JobStatus.Cancelled;
        job.CompletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}
