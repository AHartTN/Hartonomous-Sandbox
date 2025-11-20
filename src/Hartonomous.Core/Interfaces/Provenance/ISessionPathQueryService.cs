using Hartonomous.Core.Interfaces.Provenance;

namespace Hartonomous.Core.Interfaces.Provenance;

/// <summary>
/// Service for querying session reasoning paths and decision trees.
/// </summary>
public interface ISessionPathQueryService
{
    /// <summary>
    /// Gets all reasoning paths taken within a session.
    /// Shows the complete decision tree with branches explored.
    /// </summary>
    /// <param name="sessionId">The session ID to analyze.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of reasoning paths with decision points and outcomes.</returns>
    Task<IEnumerable<ReasoningPath>> GetSessionPathsAsync(
        long sessionId,
        CancellationToken cancellationToken = default);
}