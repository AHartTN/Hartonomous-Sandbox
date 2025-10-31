using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a deduplicated atomic piece of content (text, code, image patch, audio frame, tensor atom, etc.).
/// </summary>
public class Atom
{
    public long AtomId { get; set; }

    public required byte[] ContentHash { get; set; }

    public required string Modality { get; set; }

    public string? Subtype { get; set; }

    public string? SourceUri { get; set; }

    public string? SourceType { get; set; }

    public string? CanonicalText { get; set; }

    public string? PayloadLocator { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public long ReferenceCount { get; set; } = 0;

    public Point? SpatialKey { get; set; }

    public ICollection<AtomEmbedding> Embeddings { get; set; } = new List<AtomEmbedding>();

    public ICollection<TensorAtom> TensorAtoms { get; set; } = new List<TensorAtom>();

    public ICollection<AtomRelation> SourceRelations { get; set; } = new List<AtomRelation>();

    public ICollection<AtomRelation> TargetRelations { get; set; } = new List<AtomRelation>();
}
