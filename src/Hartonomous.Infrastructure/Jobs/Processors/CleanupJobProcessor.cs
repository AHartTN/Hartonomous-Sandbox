using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Jobs.Processors;

/// <summary>
/// Payload for database cleanup jobs.
/// </summary>
public class CleanupJobPayload
{
    /// <summary>
    /// Types of cleanup to perform (e.g., "OldLogs", "ExpiredCache", "TempFiles").
    /// </summary>
    public required List<string> CleanupTypes { get; set; }

    /// <summary>
    /// Retention period in days. Records older than this will be deleted.
    /// </summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Maximum number of records to delete per batch.
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}

/// <summary>
/// Result from cleanup job execution.
/// </summary>
public class CleanupJobResult
{
    public Dictionary<string, int> DeletedCounts { get; set; } = new();
    public int TotalDeleted { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Processes database cleanup jobs to remove old logs, expired cache, temp files, etc.
/// </summary>
public class CleanupJobProcessor : IJobProcessor<CleanupJobPayload>
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<CleanupJobProcessor> _logger;

    public string JobType => "Cleanup";

    public CleanupJobProcessor(
        HartonomousDbContext context,
        ILogger<CleanupJobProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<object?> ProcessAsync(
        CleanupJobPayload payload,
        JobExecutionContext context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var result = new CleanupJobResult();

        foreach (var cleanupType in payload.CleanupTypes)
        {
            var deletedCount = await ExecuteCleanupAsync(cleanupType, payload.RetentionDays, payload.BatchSize, cancellationToken);
            result.DeletedCounts[cleanupType] = deletedCount;
            result.TotalDeleted += deletedCount;

            _logger.LogInformation("Cleanup '{CleanupType}' deleted {Count} records (retention: {RetentionDays} days)",
                cleanupType, deletedCount, payload.RetentionDays);
        }

        result.Duration = DateTime.UtcNow - startTime;

        _logger.LogInformation("Cleanup job completed: {TotalDeleted} total records deleted in {DurationMs}ms",
            result.TotalDeleted, result.Duration.TotalMilliseconds);

        return result;
    }

    private async Task<int> ExecuteCleanupAsync(
        string cleanupType,
        int retentionDays,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var totalDeleted = 0;

        switch (cleanupType)
        {
            case "OldLogs":
                totalDeleted = await CleanupOldLogsAsync(cutoffDate, batchSize, cancellationToken);
                break;

            case "ExpiredCache":
                totalDeleted = await CleanupExpiredCacheAsync(cutoffDate, batchSize, cancellationToken);
                break;

            case "CompletedJobs":
                totalDeleted = await CleanupCompletedJobsAsync(cutoffDate, batchSize, cancellationToken);
                break;

            case "FailedJobs":
                totalDeleted = await CleanupFailedJobsAsync(cutoffDate, batchSize, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown cleanup type: {CleanupType}", cleanupType);
                break;
        }

        return totalDeleted;
    }

    private async Task<int> CleanupOldLogsAsync(DateTime cutoffDate, int batchSize, CancellationToken cancellationToken)
    {
        // Delete old diagnostic logs
        var sql = @"
            DELETE TOP (@BatchSize) FROM dbo.DiagnosticLogs
            WHERE CreatedAt < @CutoffDate";

        var totalDeleted = 0;
        int deletedInBatch;

        do
        {
            deletedInBatch = await _context.Database.ExecuteSqlRawAsync(
                sql,
                new object[] {
                    new SqlParameter("@BatchSize", batchSize),
                    new SqlParameter("@CutoffDate", cutoffDate)
                },
                cancellationToken);

            totalDeleted += deletedInBatch;

            if (deletedInBatch > 0)
            {
                await Task.Delay(100, cancellationToken); // Small delay between batches
            }
        }
        while (deletedInBatch == batchSize && !cancellationToken.IsCancellationRequested);

        return totalDeleted;
    }

    private async Task<int> CleanupExpiredCacheAsync(DateTime cutoffDate, int batchSize, CancellationToken cancellationToken)
    {
        // Delete expired cache entries
        var sql = @"
            DELETE TOP (@BatchSize) FROM dbo.CacheEntries
            WHERE ExpiresAt < @CutoffDate";

        var totalDeleted = 0;
        int deletedInBatch;

        do
        {
            deletedInBatch = await _context.Database.ExecuteSqlRawAsync(
                sql,
                new object[] {
                    new SqlParameter("@BatchSize", batchSize),
                    new SqlParameter("@CutoffDate", cutoffDate)
                },
                cancellationToken);

            totalDeleted += deletedInBatch;

            if (deletedInBatch > 0)
            {
                await Task.Delay(100, cancellationToken);
            }
        }
        while (deletedInBatch == batchSize && !cancellationToken.IsCancellationRequested);

        return totalDeleted;
    }

    private async Task<int> CleanupCompletedJobsAsync(DateTime cutoffDate, int batchSize, CancellationToken cancellationToken)
    {
        // Delete old completed background jobs
        var jobs = await _context.Set<BackgroundJob>()
            .Where(j => j.Status == JobStatus.Completed && j.CompletedAtUtc < cutoffDate)
            .OrderBy(j => j.CompletedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (jobs.Count > 0)
        {
            _context.Set<BackgroundJob>().RemoveRange(jobs);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return jobs.Count;
    }

    private async Task<int> CleanupFailedJobsAsync(DateTime cutoffDate, int batchSize, CancellationToken cancellationToken)
    {
        // Delete old failed/dead-lettered jobs
        var jobs = await _context.Set<BackgroundJob>()
            .Where(j => (j.Status == JobStatus.Failed || j.Status == JobStatus.DeadLettered) &&
                       j.CompletedAtUtc < cutoffDate)
            .OrderBy(j => j.CompletedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (jobs.Count > 0)
        {
            _context.Set<BackgroundJob>().RemoveRange(jobs);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return jobs.Count;
    }
}
