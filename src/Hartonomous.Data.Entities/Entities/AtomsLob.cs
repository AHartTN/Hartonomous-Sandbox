using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

/// <summary>
/// Large object storage for Atoms table. Separates LOBs to disk to enable Atoms memory-optimization.
/// </summary>
public partial class AtomsLob : IAtomsLob
{
    /// <summary>
    /// Foreign key to Atoms.AtomId. CASCADE DELETE ensures no orphaned LOBs.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Full text content for documents, articles, transcripts. Stored as NVARCHAR(MAX) for full-text indexing.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Binary payload for multimedia content (images, audio, video). Stored as VARBINARY(MAX).
    /// </summary>
    public byte[]? ComponentStream { get; set; }

    /// <summary>
    /// Extended JSON metadata. Native JSON type for SQL Server 2025.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Azure Blob Storage URL for offloaded large content. Enables hybrid storage (hot metadata in SQL, cold payload in blob).
    /// </summary>
    public string? PayloadLocator { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Atom Atom { get; set; } = null!;
}
