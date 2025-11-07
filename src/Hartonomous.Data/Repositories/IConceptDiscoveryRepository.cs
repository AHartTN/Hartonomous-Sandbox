using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Repositories;

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

/// <summary>
/// Vector with spatial information for clustering
/// </summary>
public class EmbeddingVector
{
    /// <summary>
    /// Unique identifier for the vector
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Vector coordinates
    /// </summary>
    public double[] Vector { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Spatial location in embedding space
    /// </summary>
    public Point? SpatialLocation { get; set; }

    /// <summary>
    /// Associated atom ID if applicable
    /// </summary>
    public Guid? AtomId { get; set; }

    /// <summary>
    /// Metadata about the vector source
    /// </summary>
    public string Metadata { get; set; } = string.Empty;
}

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

/// <summary>
/// A discovered concept from clustering
/// </summary>
public class DiscoveredConcept
{
    /// <summary>
    /// Unique identifier for the concept
    /// </summary>
    public Guid ConceptId { get; set; }

    /// <summary>
    /// Name/label for the concept
    /// </summary>
    public string ConceptName { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the concept represents
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Centroid vector of the cluster
    /// </summary>
    public double[] Centroid { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Vectors belonging to this concept
    /// </summary>
    public IReadOnlyList<EmbeddingVector> MemberVectors { get; set; } = Array.Empty<EmbeddingVector>();

    /// <summary>
    /// Confidence score for this concept (0-1)
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Spatial bounds of the concept cluster
    /// </summary>
    public Geometry? ClusterBounds { get; set; }
}

/// <summary>
/// Result of concept binding operation
/// </summary>
public class ConceptBindingResult
{
    /// <summary>
    /// Unique ID for this binding session
    /// </summary>
    public Guid BindingId { get; set; }

    /// <summary>
    /// Successfully bound concepts
    /// </summary>
    public IReadOnlyList<BoundConcept> BoundConcepts { get; set; } = Array.Empty<BoundConcept>();

    /// <summary>
    /// Concepts that failed to bind
    /// </summary>
    public IReadOnlyList<FailedBinding> FailedBindings { get; set; } = Array.Empty<FailedBinding>();

    /// <summary>
    /// New relationships created
    /// </summary>
    public int RelationshipsCreated { get; set; }

    /// <summary>
    /// Timestamp of binding operation
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// A successfully bound concept
/// </summary>
public class BoundConcept
{
    /// <summary>
    /// The discovered concept
    /// </summary>
    public DiscoveredConcept Concept { get; set; } = null!;

    /// <summary>
    /// ID of the created or updated concept entity
    /// </summary>
    public long ConceptEntityId { get; set; }

    /// <summary>
    /// Relationships established
    /// </summary>
    public IReadOnlyList<string> Relationships { get; set; } = Array.Empty<string>();
}

/// <summary>
/// A concept that failed to bind
/// </summary>
public class FailedBinding
{
    /// <summary>
    /// The concept that failed to bind
    /// </summary>
    public DiscoveredConcept Concept { get; set; } = null!;

    /// <summary>
    /// Reason for binding failure
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}