using NetTopologySuite.Geometries;

namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

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
