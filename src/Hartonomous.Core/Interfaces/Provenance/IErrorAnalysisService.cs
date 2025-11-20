using Hartonomous.Core.Interfaces.Provenance;

namespace Hartonomous.Core.Interfaces.Provenance;

/// <summary>
/// Service for analyzing errors and exceptions in atomization processes.
/// </summary>
public interface IErrorAnalysisService
{
    /// <summary>
    /// Gets all errors that occurred during a session's atomization processes.
    /// </summary>
    /// <param name="sessionId">The session ID to analyze errors for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of errors with context and severity information.</returns>
    Task<IEnumerable<AtomizationError>> GetSessionErrorsAsync(
        long sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds clusters of related errors in the reasoning system.
    /// Groups errors by spatial proximity, temporal correlation, and semantic similarity.
    /// </summary>
    /// <param name="sessionId">Optional: Filter to specific session</param>
    /// <param name="minClusterSize">Minimum errors per cluster</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of error clusters with spatial and temporal information.</returns>
    Task<IEnumerable<ErrorCluster>> FindErrorClustersAsync(
        long? sessionId = null,
        int minClusterSize = 3,
        CancellationToken cancellationToken = default);
}