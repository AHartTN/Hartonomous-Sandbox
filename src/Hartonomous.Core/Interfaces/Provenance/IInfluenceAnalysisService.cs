using Hartonomous.Core.Interfaces.Provenance;

namespace Hartonomous.Core.Interfaces.Provenance;

/// <summary>
/// Service for analyzing influence relationships between atoms.
/// </summary>
public interface IInfluenceAnalysisService
{
    /// <summary>
    /// Gets all atoms that influenced a given atom's creation.
    /// </summary>
    /// <param name="atomId">The atom ID to analyze influences for.</param>
    /// <param name="maxDepth">Maximum depth to traverse influence relationships.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of influence relationships with depth information.</returns>
    Task<IEnumerable<InfluenceRelationship>> GetInfluencesAsync(
        long atomId,
        int maxDepth = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets atoms that influenced the creation or transformation of a specific result atom.
    /// Returns weighted influence scores and spatial relationships.
    /// </summary>
    /// <param name="atomId">The result atom to analyze</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of atoms that influenced the result atom.</returns>
    Task<IEnumerable<AtomInfluence>> GetInfluencingAtomsAsync(
        long atomId,
        CancellationToken cancellationToken = default);
}