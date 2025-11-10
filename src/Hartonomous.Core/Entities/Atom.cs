using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a deduplicated, content-addressable, atomic unit of information within the Hartonomous ecosystem.
/// Atoms are the fundamental building blocks for all data (text, images, audio, code, even AI models themselves).
/// This entity defines the core metadata and relationships for an atom, while the binary payload is typically
/// stored separately in the AtomPayloadStore to optimize for SQL Server FILESTREAM performance.
/// </summary>
public class Atom
{
    /// <summary>
    /// The unique, surrogate primary key for the atom.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// A SHA256 hash of the atom's raw binary content. This serves as the content-addressable key
    /// and is the foundation of the system's deduplication strategy. Before any new atom is created,
    /// the system checks for an existing atom with the same ContentHash.
    /// EF Core Configuration: Maps to a unique index (UX_Atoms_ContentHash).
    /// </summary>
    public required byte[] ContentHash { get; set; }

    /// <summary>
    /// The primary modality of the atom's content, such as 'text', 'image', 'audio', 'code', or 'tensor'.
    /// This high-level classification governs which processing pipelines (e.g., sp_AtomizeImage) are applied.
    /// </summary>
    public required string Modality { get; set; }

    /// <summary>
    /// A specific subtype within the modality, providing more granular detail.
    /// Examples: 'markdown' for text, 'jpeg' for image, 'python' for code, 'float32' for tensor.
    /// </summary>
    public string? Subtype { get; set; }

    /// <summary>
    /// The original source URI from which this atom's content was ingested. This is critical for provenance,
    /// allowing the system to trace any piece of information back to its origin (e.g., a file path, a URL).
    /// </summary>
    public string? SourceUri { get; set; }

    /// <summary>
    /// The type of the source URI, such as 'file', 'url', 'database', or 'api'.
    /// Provides context for how the SourceUri should be interpreted.
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// The canonical textual representation of the atom's content. For a 'text' atom, this is the text itself.
    /// For other modalities like 'image', it could be a descriptive caption, OCR text, or null.
    /// This field is often used for full-text indexing and as input to embedding models.
    /// </summary>
    public string? CanonicalText { get; set; }

    /// <summary>
    /// A locator for the atom's binary payload if it is stored outside of the primary database.
    /// For default FILESTREAM storage, this is typically null. For a hybrid storage model using
    /// Azure Blob Storage, this would hold the blob's URI.
    /// </summary>
    public string? PayloadLocator { get; set; }

    /// <summary>
    /// A flexible JSON field for storing additional, modality-specific metadata.
    /// For an image, this might contain dimensions and color depth. For a video, frame rate.
    /// For a tensor, the shape and data type.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// The UTC timestamp when the atom was first created in the database.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The UTC timestamp of the last update to this atom's metadata or reference count.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Multi-tenant isolation identifier. Defaults to 0 for single-tenant or default tenant.
    /// </summary>
    public int TenantId { get; set; } = 0;

    /// <summary>
    /// Soft-delete flag for atoms. When true, the atom is logically deleted but retained for audit/provenance.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// The UTC timestamp when the atom was soft-deleted, if applicable.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// A flag indicating if the atom is active and available for querying.
    /// Inactive atoms can be excluded from searches and may be candidates for archival or garbage collection.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// A simple reference counter used for basic garbage collection and importance scoring.
    /// Incremented when a new entity references this atom.
    /// </summary>
    public long ReferenceCount { get; set; } = 0;

    /// <summary>
    /// The 3D spatial projection of the atom's primary embedding vector. This is a critical field
    /// for performance, enabling fast approximate nearest neighbor (ANN) searches by turning a high-dimensional
    /// vector search into a 3D spatial query.
    /// EF Core Configuration: Maps to a GEOMETRY column with a spatial index.
    /// </summary>
    public Point? SpatialKey { get; set; }

    /// <summary>
    /// A binary stream encoding dense sub-atomic features or coefficients, used for advanced
    /// decomposition and reconstruction of complex atoms. For example, it could store wavelet
    /// coefficients for an image or audio frame.
    /// </summary>
    public byte[]? ComponentStream { get; set; }

    /// <summary>
    /// Navigation property for the collection of embeddings associated with this atom.
    /// An atom can have multiple embeddings from different AI models.
    /// </summary>
    public ICollection<AtomEmbedding> Embeddings { get; set; } = new List<AtomEmbedding>();

    /// <summary>
    /// Navigation property for the collection of tensor atoms (e.g., model weights) associated with this atom.
    /// This is how the system represents AI models as data within the database itself.
    /// </summary>
    public ICollection<TensorAtom> TensorAtoms { get; set; } = new List<TensorAtom>();

    /// <summary>
    /// Navigation property for the collection of graph relationships where this atom is the source node.
    /// </summary>
    public ICollection<AtomRelation> SourceRelations { get; set; } = new List<AtomRelation>();

    /// <summary>
    /// Navigation property for the collection of graph relationships where this atom is the target node.
    /// </summary>
    public ICollection<AtomRelation> TargetRelations { get; set; } = new List<AtomRelation>();
}

