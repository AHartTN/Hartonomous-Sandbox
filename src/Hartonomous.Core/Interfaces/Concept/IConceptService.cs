using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Concept;

/// <summary>
/// Service for concept discovery and binding operations.
/// </summary>
public interface IConceptService
{
    /// <summary>
    /// Discovers concepts via DBSCAN clustering and binds atoms to concepts.
    /// Calls sp_DiscoverAndBindConcepts stored procedure.
    /// </summary>
    Task<ConceptDiscoveryResult> DiscoverAndBindAsync(
        int minClusterSize = 10,
        float coherenceThreshold = 0.7f,
        int maxConcepts = 100,
        float similarityThreshold = 0.6f,
        int maxConceptsPerAtom = 5,
        int tenantId = 0,
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds Voronoi-like semantic domains for concepts.
    /// Calls sp_BuildConceptDomains stored procedure.
    /// </summary>
    Task BuildDomainsAsync(
        int tenantId = 0,
        CancellationToken cancellationToken = default);
}

public record ConceptDiscoveryResult(
    int ConceptsDiscovered,
    int AtomsProcessed,
    int BindingsCreated,
    IEnumerable<DiscoveredConcept> Concepts);

public record DiscoveredConcept(
    int ConceptId,
    string? Name,
    int ClusterSize,
    float CoherenceScore,
    string? TopTerms);
