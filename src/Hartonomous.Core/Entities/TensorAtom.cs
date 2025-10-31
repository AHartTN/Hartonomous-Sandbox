using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a reusable tensor atom (kernel, basis vector, attention head slice) derived from a larger tensor.
/// </summary>
public class TensorAtom
{
    public long TensorAtomId { get; set; }

    public long AtomId { get; set; }

    public int? ModelId { get; set; }

    public long? LayerId { get; set; }

    public required string AtomType { get; set; }

    public Point? SpatialSignature { get; set; }

    public Geometry? GeometryFootprint { get; set; }

    public string? Metadata { get; set; }

    public float? ImportanceScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Atom Atom { get; set; } = null!;

    public Model? Model { get; set; }

    public ModelLayer? Layer { get; set; }

    public ICollection<TensorAtomCoefficient> Coefficients { get; set; } = new List<TensorAtomCoefficient>();
}
