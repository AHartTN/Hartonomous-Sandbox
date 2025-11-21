using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Hartonomous.Infrastructure.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly string _connectionString;

    public BackgroundJobService(
        HartonomousDbContext context,
        ILogger<BackgroundJobService> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("HartonomousDb") 
            ?? throw new InvalidOperationException("HartonomousDb connection string not configured");
    }

    public async Task<Guid> CreateJobAsync(
        string jobType,
        string parametersJson,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var job = new Data.Entities.Entities.BackgroundJob
        {
            JobId = Guid.NewGuid(),
            JobType = jobType,
            Parameters = parametersJson,
            TenantId = tenantId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.BackgroundJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created background job: JobId={JobId}, Type={JobType}, TenantId={TenantId}",
            job.JobId, jobType, tenantId);

        return job.JobId;
    }

    public async Task<BackgroundJobInfo?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);

        if (job == null)
            return null;

        return new BackgroundJobInfo(
            job.JobId,
            job.JobType,
            job.Status,
            job.Parameters,
            job.Result,
            null, // ErrorMessage not in entity
            job.TenantId,
            job.CreatedAt,
            job.CompletedAt);
    }

    public async Task UpdateJobAsync(
        Guid jobId,
        string status,
        string? resultJson = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job not found: JobId={JobId}", jobId);
            return;
        }

        job.Status = status;
        job.Result = resultJson ?? errorMessage; // Store error in Result field if present
        job.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated job status: JobId={JobId}, Status={Status}",
            jobId, status);
    }

    public async Task<IEnumerable<BackgroundJobInfo>> ListJobsAsync(
        int tenantId,
        string? statusFilter = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BackgroundJobs
            .Where(j => j.TenantId == tenantId);

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(j => j.Status == statusFilter);
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return jobs.Select(j => new BackgroundJobInfo(
            j.JobId,
            j.JobType,
            j.Status,
            j.Parameters,
            j.Result,
            null,
            j.TenantId,
            j.CreatedAt,
            j.CompletedAt));
    }

    public async Task EnqueueIngestionAsync(
        string atomJson,
        int tenantId,
        int priority = 5,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_EnqueueIngestion", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@atomJson", atomJson);
        command.Parameters.AddWithValue("@tenantId", tenantId);
        command.Parameters.AddWithValue("@priority", priority);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "Enqueued ingestion job: TenantId={TenantId}, Priority={Priority}",
            tenantId, priority);
    }

    public async Task EnqueueNeo4jSyncAsync(
        string entityType,
        long entityId,
        string syncType = "CREATE",
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_EnqueueNeo4jSync", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@entityType", entityType);
        command.Parameters.AddWithValue("@entityId", entityId);
        command.Parameters.AddWithValue("@syncType", syncType);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "Enqueued Neo4j sync: EntityType={EntityType}, EntityId={EntityId}, SyncType={SyncType}",
            entityType, entityId, syncType);
    }

    /// <summary>
    /// Gets pending jobs for a specific job type (for workers to poll)
    /// </summary>
    public async Task<IEnumerable<(Guid JobId, string Parameters)>> GetPendingJobsAsync(
        string jobType,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _context.BackgroundJobs
            .Where(j => j.JobType == jobType && j.Status == "Pending")
            .OrderBy(j => j.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return jobs.Select(j => (j.JobId, j.Parameters));
    }
}
