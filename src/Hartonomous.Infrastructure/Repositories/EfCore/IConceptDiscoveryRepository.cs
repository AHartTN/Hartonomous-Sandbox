using Hartonomous.Infrastructure.Repositories.EfCore.Models;

namespace Hartonomous.Infrastructure.Repositories.EfCore;

/// <summary>
/// Interface for concept discovery and binding operations
/// Replaces sp_DiscoverAndBindConcepts stored procedure
/// </summary>
public interface IConceptDiscoveryRepository
{
    /// <summary>
    /// Discovers concepts through clustering analysis
    /// </summary>
    /// <param name="embeddingVectors">Vectors to analyze for clustering</param>
    /// <param name="minClusterSize">Minimum size for a valid cluster</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discovered concepts with their clusters</returns>
    Task<ConceptDiscoveryResult> DiscoverConceptsAsync(
        IReadOnlyList<EmbeddingVector> embeddingVectors,
        int minClusterSize = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Binds discovered concepts to existing knowledge graph
    /// </summary>
    /// <param name="concepts">Concepts to bind</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Binding results</returns>
    Task<ConceptBindingResult> BindConceptsAsync(
        IReadOnlyList<DiscoveredConcept> concepts,
        CancellationToken cancellationToken = default);
}
