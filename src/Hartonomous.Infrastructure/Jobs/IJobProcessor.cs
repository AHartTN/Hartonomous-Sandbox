namespace Hartonomous.Infrastructure.Jobs;

/// <summary>
/// Interface for background job processors.
/// Implement this interface to create custom job types.
/// </summary>
/// <typeparam name="TPayload">The type of the job payload.</typeparam>
public interface IJobProcessor<TPayload> where TPayload : class
{
    /// <summary>
    /// Gets the job type identifier this processor handles.
    /// </summary>
    string JobType { get; }

    /// <summary>
    /// Processes the job with the given payload.
    /// </summary>
    /// <param name="payload">Deserialized job payload.</param>
    /// <param name="context">Execution context with metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result data to be serialized and stored, or null.</returns>
    Task<object?> ProcessAsync(TPayload payload, JobExecutionContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Context provided to job processors during execution.
/// </summary>
public class JobExecutionContext
{
    /// <summary>
    /// Job ID being processed.
    /// </summary>
    public long JobId { get; init; }

    /// <summary>
    /// Current attempt number (1-based).
    /// </summary>
    public int AttemptNumber { get; init; }

    /// <summary>
    /// Maximum retry attempts configured.
    /// </summary>
    public int MaxRetries { get; init; }

    /// <summary>
    /// Tenant ID if multi-tenant job.
    /// </summary>
    public int? TenantId { get; init; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// User/service that created the job.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// When the job was originally created.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }
}
