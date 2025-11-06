using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a deduplicated atomic piece of content (text, code, image patch, audio frame, tensor atom, etc.).
/// Atoms are the fundamental unit of content-addressable storage in the system, enabling deduplication and reuse.
/// Each atom is uniquely identified by its content hash and can have multiple embeddings and relationships.
/// </summary>
public class Atom
{
    /// <summary>
    /// Gets or sets the unique identifier for the atom.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the atom's content, serving as a content-addressable key for deduplication.
    /// </summary>
    public required byte[] ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the modality of the atom (e.g., 'text', 'image', 'audio', 'code', 'tensor').
    /// </summary>
    public required string Modality { get; set; }

    /// <summary>
    /// Gets or sets the subtype within the modality (e.g., 'markdown', 'jpeg', 'wav', 'python').
    /// </summary>
    public string? Subtype { get; set; }

    /// <summary>
    /// Gets or sets the URI from which this atom originated, if applicable.
    /// </summary>
    public string? SourceUri { get; set; }

    /// <summary>
    /// Gets or sets the type of source (e.g., 'file', 'url', 'database', 'api').
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// Gets or sets the canonical textual representation of the atom's content, if applicable.
    /// For text atoms, this is the actual text; for other modalities, it may be a description or null.
    /// </summary>
    public string? CanonicalText { get; set; }

    /// <summary>
    /// Gets or sets a locator string pointing to the full payload if content is stored externally (e.g., blob storage path).
    /// </summary>
    public string? PayloadLocator { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., dimensions, format, encoding parameters).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the atom was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the atom was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the atom is active and available for use.
    /// Inactive atoms may be marked for deletion or archival.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of references to this atom from other entities.
    /// Used for garbage collection and importance scoring.
    /// </summary>
    public long ReferenceCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the spatial key (geography point) for geospatial queries or spatial indexing of the atom.
    /// </summary>
    public Point? SpatialKey { get; set; }

    /// <summary>
    /// Gets or sets the component stream encoding dense sub-atomic features or coefficients.
    /// Used for advanced decomposition and reconstruction of complex atoms.
    /// </summary>
    public byte[]? ComponentStream { get; set; }

    /// <summary>
    /// Gets or sets the collection of embeddings associated with this atom across different models.
    /// </summary>
    public ICollection<AtomEmbedding> Embeddings { get; set; } = new List<AtomEmbedding>();

    /// <summary>
    /// Gets or sets the collection of tensor atoms derived from or associated with this atom.
    /// </summary>
    public ICollection<TensorAtom> TensorAtoms { get; set; } = new List<TensorAtom>();

    /// <summary>
    /// Gets or sets the collection of relations where this atom is the source.
    /// </summary>
    public ICollection<AtomRelation> SourceRelations { get; set; } = new List<AtomRelation>();

    /// <summary>
    /// Gets or sets the collection of relations where this atom is the target.
    /// </summary>
    public ICollection<AtomRelation> TargetRelations { get; set; } = new List<AtomRelation>();
}
