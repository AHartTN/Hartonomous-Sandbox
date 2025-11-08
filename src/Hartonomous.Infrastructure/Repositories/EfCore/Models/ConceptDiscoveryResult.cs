namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Result of concept discovery operation
/// </summary>
public class ConceptDiscoveryResult
{
    /// <summary>
    /// Unique ID for this discovery session
    /// </summary>
    public Guid DiscoveryId { get; set; }

    /// <summary>
    /// Discovered concepts
    /// </summary>
    public IReadOnlyList<DiscoveredConcept> Concepts { get; set; } = Array.Empty<DiscoveredConcept>();

    /// <summary>
    /// Total vectors processed
    /// </summary>
    public int VectorsProcessed { get; set; }

    /// <summary>
    /// Number of clusters found
    /// </summary>
    public int ClustersFound { get; set; }

    /// <summary>
    /// Quality score of clustering (0-1)
    /// </summary>
    public double ClusteringQuality { get; set; }

    /// <summary>
    /// Timestamp of discovery
    /// </summary>
    public DateTime Timestamp { get; set; }
}
