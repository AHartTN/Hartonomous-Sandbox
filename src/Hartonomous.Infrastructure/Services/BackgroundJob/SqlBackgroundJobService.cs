using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.BackgroundJob;

/// <summary>
/// SQL Server implementation of background job service.
/// Replaces in-memory job tracking with persistent storage.
/// </summary>
public sealed class SqlBackgroundJobService : IBackgroundJobService
{
    private readonly HartonomousDbContext _context;
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlBackgroundJobService> _logger;

    public SqlBackgroundJobService(
        HartonomousDbContext context,
        ILogger<SqlBackgroundJobService> logger,
        IOptions<DatabaseOptions> options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<Guid> CreateJobAsync(
        string jobType,
        string parametersJson,
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var job = new BackgroundJob
        {
            JobType = jobType,
            Status = 0, // Pending
            Payload = parametersJson,
            TenantId = tenantId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.BackgroundJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created background job {JobId} of type {JobType}", job.JobId, jobType);
        
        // Return a deterministic Guid based on the JobId for backwards compatibility
        var guidBytes = new byte[16];
        BitConverter.GetBytes(job.JobId).CopyTo(guidBytes, 0);
        return new Guid(guidBytes);
    }

    public async Task<BackgroundJobInfo?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        // Convert Guid back to long JobId
        var longJobId = BitConverter.ToInt64(jobId.ToByteArray(), 0);
        
        var job = await _context.BackgroundJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.JobId == longJobId, cancellationToken);

        if (job == null) return null;

        var statusString = job.Status switch
        {
            0 => "Pending",
            1 => "InProgress",
            2 => "Completed",
            3 => "Failed",
            4 => "DeadLettered",
            5 => "Cancelled",
            6 => "Scheduled",
            _ => "Unknown"
        };

        var guidBytes = new byte[16];
        BitConverter.GetBytes(job.JobId).CopyTo(guidBytes, 0);
        var resultGuid = new Guid(guidBytes);

        return new BackgroundJobInfo(
            resultGuid,
            job.JobType,
            statusString,
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
        // Convert Guid back to long JobId
        var longJobId = BitConverter.ToInt64(jobId.ToByteArray(), 0);
        
        var job = await _context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.JobId == longJobId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found for update", jobId);
            return;
        }

        job.Status = status switch
        {
            "Pending" => 0,
            "InProgress" => 1,
            "Completed" => 2,
            "Failed" => 3,
            "DeadLettered" => 4,
            "Cancelled" => 5,
            "Scheduled" => 6,
            _ => job.Status
        };
        job.ResultData = resultJson;
        job.ErrorMessage = errorMessage;

        if (status == "Completed" || status == "Failed")
        {
            job.CompletedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated job {JobId} to status {Status}", jobId, status);
    }

    public async Task<IEnumerable<BackgroundJobInfo>> ListJobsAsync(
        int tenantId,
        string? statusFilter = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BackgroundJobs
            .AsNoTracking()
            .Where(j => j.TenantId == tenantId);

        if (!string.IsNullOrEmpty(statusFilter))
        {
            var statusInt = statusFilter switch
            {
                "Pending" => 0,
                "InProgress" => 1,
                "Completed" => 2,
                "Failed" => 3,
                "DeadLettered" => 4,
                "Cancelled" => 5,
                "Scheduled" => 6,
                _ => -1
            };
            
            if (statusInt >= 0)
            {
                query = query.Where(j => j.Status == statusInt);
            }
        }

        var jobs = await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return jobs.Select(j =>
        {
            var statusString = j.Status switch
            {
                0 => "Pending",
                1 => "InProgress",
                2 => "Completed",
                3 => "Failed",
                4 => "DeadLettered",
                5 => "Cancelled",
                6 => "Scheduled",
                _ => "Unknown"
            };

            var guidBytes = new byte[16];
            BitConverter.GetBytes(j.JobId).CopyTo(guidBytes, 0);
            var resultGuid = new Guid(guidBytes);

            return new BackgroundJobInfo(
                resultGuid,
                j.JobType,
                statusString,
                j.Payload,
                j.ResultData,
                j.ErrorMessage,
                j.TenantId ?? 0,
                j.CreatedAtUtc,
                j.CompletedAtUtc);
        });
    }

    public async Task EnqueueIngestionAsync(
        string atomJson,
        int tenantId,
        int priority = 5,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_EnqueueIngestion", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@AtomJson", atomJson);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@Priority", priority);

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Enqueued ingestion job for tenant {TenantId}", tenantId);
    }

    public async Task EnqueueNeo4jSyncAsync(
        string entityType,
        long entityId,
        string syncType = "CREATE",
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_EnqueueNeo4jSync", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@EntityType", entityType);
        command.Parameters.AddWithValue("@EntityId", entityId);
        command.Parameters.AddWithValue("@SyncType", syncType);

        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("Enqueued Neo4j sync for {EntityType} {EntityId}", entityType, entityId);
    }

    public async Task<IEnumerable<(Guid JobId, string ParametersJson)>> GetPendingJobsAsync(
        string jobType,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _context.BackgroundJobs
            .AsNoTracking()
            .Where(j => j.JobType == jobType && j.Status == 0) // 0 = Pending
            .OrderBy(j => j.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return jobs.Select(j =>
        {
            var guidBytes = new byte[16];
            BitConverter.GetBytes(j.JobId).CopyTo(guidBytes, 0);
            return (JobId: new Guid(guidBytes), ParametersJson: j.Payload ?? "{}");
        });
    }

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (!_connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !_connectionString.Contains("Integrated Security=true", StringComparison.OrdinalIgnoreCase))
        {
            var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
            var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync(cancellationToken);
    }
}
