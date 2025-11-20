using Hartonomous.Core.Interfaces.Provenance;

namespace Hartonomous.Core.Interfaces.Provenance;

/// <summary>
/// Service for querying atom lineage and ancestry information.
/// </summary>
public interface ILineageQueryService
{
    /// <summary>
    /// Gets the complete lineage (ancestry chain) for a specific atom.
    /// </summary>
    /// <param name="atomId">The atom ID to trace lineage for.</param>
    /// <param name="maxDepth">Maximum depth to traverse (default: unlimited).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The lineage tree showing all ancestor atoms and relationships.</returns>
    Task<AtomLineage> GetAtomLineageAsync(
        long atomId,
        int? maxDepth = null,
        CancellationToken cancellationToken = default);
}