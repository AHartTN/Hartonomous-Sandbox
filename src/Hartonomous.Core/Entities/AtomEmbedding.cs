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

    public Point? SpatialGeometry { get; set; }

    public Point? SpatialCoarse { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Atom Atom { get; set; } = null!;

    public Model? Model { get; set; }

    public ICollection<AtomEmbeddingComponent> Components { get; set; } = new List<AtomEmbeddingComponent>();
}
