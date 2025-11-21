using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.BackgroundJob;

/// <summary>
/// Service for background job management.
/// Replaces in-memory job tracking with database persistence.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Creates a new background job.
    /// </summary>
    Task<Guid> CreateJobAsync(
        string jobType,
        string parametersJson,
        int tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets job by ID.
    /// </summary>
    Task<BackgroundJobInfo?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates job status.
    /// </summary>
    Task UpdateJobAsync(
        Guid jobId,
        string status,
        string? resultJson = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists jobs by tenant and optional status filter.
    /// </summary>
    Task<IEnumerable<BackgroundJobInfo>> ListJobsAsync(
        int tenantId,
        string? statusFilter = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues atom ingestion job.
    /// Calls sp_EnqueueIngestion stored procedure.
    /// </summary>
    Task EnqueueIngestionAsync(
        string atomJson,
        int tenantId,
        int priority = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues Neo4j sync job.
    /// Calls sp_EnqueueNeo4jSync stored procedure.
    /// </summary>
    Task EnqueueNeo4jSyncAsync(
        string entityType,
        long entityId,
        string syncType = "CREATE",
        CancellationToken cancellationToken = default);
}

public record BackgroundJobInfo(
    Guid JobId,
    string JobType,
    string Status,
    string? ParametersJson,
    string? ResultJson,
    string? ErrorMessage,
    int TenantId,
    DateTime CreatedAt,
    DateTime? CompletedAt);
