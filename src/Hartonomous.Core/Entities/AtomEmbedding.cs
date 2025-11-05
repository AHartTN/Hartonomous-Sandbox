using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents an embedding associated with an atom for a given model or embedding generator.
/// </summary>
public class AtomEmbedding
{
    public long AtomEmbeddingId { get; set; }

    public long AtomId { get; set; }

    public int? ModelId { get; set; }

    public required string EmbeddingType { get; set; }

    public int Dimension { get; set; }

    public SqlVector<float>? EmbeddingVector { get; set; }

    /// <summary>
    /// Indicates whether the stored <see cref="EmbeddingVector"/> has been padded up to the SQL maximum dimension (1998 floats).
    /// </summary>
    public bool UsesMaxDimensionPadding { get; set; }

    /// <summary>
    /// Gets or sets the X component of the computed spatial projection.
    /// Stored redundantly to avoid repeatedly materializing geometry coordinates.
    /// </summary>
    public double? SpatialProjX { get; set; }

    /// <summary>
    /// Gets or sets the Y component of the computed spatial projection.
    /// </summary>
    public double? SpatialProjY { get; set; }

    /// <summary>
    /// Gets or sets the Z component of the computed spatial projection when available.
    /// </summary>
    public double? SpatialProjZ { get; set; }

    public Point? SpatialGeometry { get; set; }

    public Point? SpatialCoarse { get; set; }

    /// <summary>
    /// Gets or sets the bucketed X coordinate (integer rounding of <see cref="SpatialProjX"/>).
    /// Enables fast joins with aggregated spatial metadata.
    /// </summary>
    public int? SpatialBucketX { get; set; }

    /// <summary>
    /// Gets or sets the bucketed Y coordinate (integer rounding of <see cref="SpatialProjY"/>).
    /// </summary>
    public int? SpatialBucketY { get; set; }

    /// <summary>
    /// Gets or sets the bucketed Z coordinate (integer rounding of <see cref="SpatialProjZ"/> when present).
    /// When null Z is not projected, the value is persisted as <see cref="int.MinValue"/> so bucket-based aggregations remain unique.
    /// </summary>
    public int SpatialBucketZ { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Atom Atom { get; set; } = null!;

    public Model? Model { get; set; }

    public ICollection<AtomEmbeddingComponent> Components { get; set; } = new List<AtomEmbeddingComponent>();
}
