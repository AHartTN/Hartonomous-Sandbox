using Hartonomous.Data.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository for managing inference requests and their lifecycle.
/// </summary>
public interface IInferenceRequestRepository
{
    /// <summary>
    /// Gets an inference request by ID.
    /// </summary>
    Task<InferenceRequest?> GetByIdAsync(long inferenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inference requests by correlation ID for distributed tracing.
    /// </summary>
    Task<IReadOnlyList<InferenceRequest>> GetByCorrelationIdAsync(
        string correlationId,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending inference requests that need processing.
    /// </summary>
    Task<IReadOnlyList<InferenceRequest>> GetPendingAsync(
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new inference request.
    /// </summary>
    Task<InferenceRequest> AddAsync(InferenceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an inference request.
    /// </summary>
    Task UpdateStatusAsync(
        long inferenceId,
        string status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the result of a completed inference request.
    /// </summary>
    Task UpdateResultAsync(
        long inferenceId,
        object result,
        double confidence,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inference requests with confidence above a threshold.
    /// </summary>
    Task<IReadOnlyList<InferenceRequest>> GetHighConfidenceAsync(
        double minConfidence,
        int take = 100,
        CancellationToken cancellationToken = default);
}
