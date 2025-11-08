using NetTopologySuite.Geometries;

namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

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
