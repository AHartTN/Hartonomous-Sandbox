using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a relationship between two atoms (temporal order, attention, composition, etc.).
/// </summary>
public class AtomRelation
{
    public long AtomRelationId { get; set; }

    public long SourceAtomId { get; set; }

    public long TargetAtomId { get; set; }

    public required string RelationType { get; set; }

    public float? Weight { get; set; }

    public Geometry? SpatialExpression { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Atom SourceAtom { get; set; } = null!;

    public Atom TargetAtom { get; set; } = null!;
}
