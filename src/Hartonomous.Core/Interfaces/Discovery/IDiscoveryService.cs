using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Discovery;

/// <summary>
/// Discovery service for unsupervised concept learning and clustering.
/// Automatically identifies patterns and concepts from atom relationships.
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Cluster atoms into concepts using unsupervised learning.
    /// Calls sp_ClusterConcepts stored procedure.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="minClusterSize">Minimum atoms per cluster</param>
    /// <param name="maxClusters">Maximum number of clusters to create</param>
    /// <param name="densityThreshold">Minimum cluster density (0.0-1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discovered concepts</returns>
    Task<ConceptClusteringResult> ClusterConceptsAsync(
        int tenantId = 0,
        int minClusterSize = 5,
        int maxClusters = 20,
        float densityThreshold = 0.3f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discover and bind concepts to atoms.
    /// Calls sp_DiscoverAndBindConcepts stored procedure.
    /// </summary>
    /// <param name="atomId">Atom to discover concepts for</param>
    /// <param name="maxConcepts">Maximum concepts to bind</param>
    /// <param name="confidenceThreshold">Minimum binding confidence</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discovered concept bindings</returns>
    Task<IEnumerable<ConceptBinding>> DiscoverAndBindAsync(
        long atomId,
        int maxConcepts = 10,
        float confidenceThreshold = 0.5f,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Build concept domain hierarchies.
    /// Calls sp_BuildConceptDomains stored procedure.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="maxDepth">Maximum hierarchy depth</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Concept domain structure</returns>
    Task<ConceptDomainResult> BuildDomainsAsync(
        int tenantId = 0,
        int maxDepth = 5,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of concept clustering operation.
/// </summary>
/// <param name="ClustersCreated">Number of concept clusters created</param>
/// <param name="AtomsProcessed">Total atoms analyzed</param>
/// <param name="AverageClusterSize">Mean atoms per cluster</param>
/// <param name="AverageDensity">Mean cluster density</param>
/// <param name="ProcessingTimeMs">Processing duration</param>
public record ConceptClusteringResult(
    int ClustersCreated,
    int AtomsProcessed,
    float AverageClusterSize,
    float AverageDensity,
    int ProcessingTimeMs);

/// <summary>
/// Binding between atom and discovered concept.
/// </summary>
/// <param name="ConceptId">Concept identifier</param>
/// <param name="ConceptName">Concept name</param>
/// <param name="Confidence">Binding confidence (0.0-1.0)</param>
/// <param name="Relevance">Relevance score</param>
public record ConceptBinding(
    int ConceptId,
    string ConceptName,
    float Confidence,
    float Relevance);

/// <summary>
/// Result of concept domain building.
/// </summary>
/// <param name="DomainsCreated">Number of domains created</param>
/// <param name="MaxDepth">Maximum hierarchy depth achieved</param>
/// <param name="TotalConcepts">Total concepts organized</param>
public record ConceptDomainResult(
    int DomainsCreated,
    int MaxDepth,
    int TotalConcepts);
