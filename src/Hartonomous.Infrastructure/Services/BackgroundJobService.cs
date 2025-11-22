using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _connectionString = configuration["ConnectionStrings:HartonomousDb"] 
            ?? throw new InvalidOperationException("HartonomousDb connection string not configured");
    }

    public async Task<Guid> CreateJobAsync(
        string jobType,
        string parametersJson,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();
        
        var job = new BackgroundJob
        {
            JobType = jobType,
            Payload = parametersJson,
            TenantId = tenantId,
            Status = 0, // Pending
            Priority = 5,
            MaxRetries = 3,
            AttemptCount = 0,
            CreatedAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId.ToString()
        };

        _context.BackgroundJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created background job: JobId={JobId}, Type={JobType}, TenantId={TenantId}",
            job.JobId, jobType, tenantId);

        return correlationId;
    }

    public async Task<BackgroundJobInfo?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.CorrelationId == jobId.ToString(), cancellationToken);

        if (job == null)
            return null;

        return new BackgroundJobInfo(
            Guid.Parse(job.CorrelationId ?? Guid.Empty.ToString()),
            job.JobType,
            GetStatusString(job.Status),
            job.Payload,
            job.ResultData,
            job.ErrorMessage,
            job.TenantId ?? 0,
            job.CreatedAtUtc,
            job.CompletedAtUtc);
    }

    public async Task UpdateJobAsync(
        Guid jobId,
        string status,
        string? resultJson = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.CorrelationId == jobId.ToString(), cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job not found: JobId={JobId}", jobId);
            return;
        }

        job.Status = GetStatusInt(status);
        job.ResultData = resultJson;
        job.ErrorMessage = errorMessage;
        job.CompletedAtUtc = DateTime.UtcNow;

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
            query = query.Where(j => j.Status == GetStatusInt(statusFilter));
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return jobs.Select(j => new BackgroundJobInfo(
            Guid.Parse(j.CorrelationId ?? Guid.Empty.ToString()),
            j.JobType,
            GetStatusString(j.Status),
            j.Payload,
            j.ResultData,
            j.ErrorMessage,
            j.TenantId ?? 0,
            j.CreatedAtUtc,
            j.CompletedAtUtc));
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
    public async Task<IEnumerable<(Guid JobId, string ParametersJson)>> GetPendingJobsAsync(
        string jobType,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _context.BackgroundJobs
            .Where(j => j.JobType == jobType && j.Status == 0) // 0 = Pending
            .OrderBy(j => j.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return jobs.Select(j => (
            JobId: Guid.Parse(j.CorrelationId ?? Guid.Empty.ToString()), 
            ParametersJson: j.Payload ?? "{}"));
    }

    private static string GetStatusString(int status) => status switch
    {
        0 => "Pending",
        1 => "Running",
        2 => "Completed",
        3 => "Failed",
        _ => "Unknown"
    };

    private static int GetStatusInt(string status) => status.ToLowerInvariant() switch
    {
        "pending" => 0,
        "running" => 1,
        "completed" => 2,
        "failed" => 3,
        _ => 0
    };
}
