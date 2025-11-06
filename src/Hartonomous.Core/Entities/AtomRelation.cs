using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a directed relationship between two atoms (temporal order, attention, composition, etc.).
/// Relations form a graph structure enabling provenance tracking, attention mapping, and compositional reasoning.
/// </summary>
public class AtomRelation
{
    /// <summary>
    /// Gets or sets the unique identifier for the atom relation.
    /// </summary>
    public long AtomRelationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the source atom in the relationship.
    /// </summary>
    public long SourceAtomId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the target atom in the relationship.
    /// </summary>
    public long TargetAtomId { get; set; }

    /// <summary>
    /// Gets or sets the type of relationship (e.g., 'next', 'attention', 'derived_from', 'composed_of').
    /// </summary>
    public required string RelationType { get; set; }

    /// <summary>
    /// Gets or sets the strength or importance of the relationship (0.0 to 1.0), if applicable.
    /// For attention relations, this represents attention weight; for temporal sequences, it may be null.
    /// </summary>
    public float? Weight { get; set; }

    /// <summary>
    /// Gets or sets a spatial geometry representing the relationship's geographic or embedding space expression.
    /// Can be used for linestring paths, polygonal regions, or complex geometric relationships.
    /// </summary>
    public Geometry? SpatialExpression { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., confidence, timestamps, provenance details).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the relation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the source atom navigation property.
    /// </summary>
    public Atom SourceAtom { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target atom navigation property.
    /// </summary>
    public Atom TargetAtom { get; set; } = null!;
}
