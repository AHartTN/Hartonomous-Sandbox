using System;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a generation operation with complete provenance tracking.
/// Each generation produces a stream of atoms tracked via AtomicStream provenance.
/// </summary>
public class GenerationStream
{
    /// <summary>
    /// Gets or sets the unique identifier for the generation stream (GUID-based primary key).
    /// </summary>
    public Guid StreamId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the auto-incrementing ID for easier referencing in CLR functions.
    /// </summary>
    public long GenerationStreamId { get; set; }

    /// <summary>
    /// Gets or sets the model ID used for generation.
    /// </summary>
    public int? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the scope or context of the generation (e.g., 'inference', 'batch', 'interactive').
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets the name or identifier of the model used for generation (legacy field).
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of generated atom IDs.
    /// Example: "1234,1235,1236"
    /// </summary>
    public string? GeneratedAtomIds { get; set; }

    /// <summary>
    /// Gets or sets the complete provenance stream (AtomicStream UDT stored as binary).
    /// Tracks every input, intermediate step, and generated component with timestamps.
    /// </summary>
    public byte[]? ProvenanceStream { get; set; }

    /// <summary>
    /// Gets or sets the context metadata as JSON.
    /// Example: {"temperature": 0.7, "topK": 50, "topP": 0.9, "requestId": "abc123"}
    /// </summary>
    public string? ContextMetadata { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenancy isolation.
    /// Default: 0 (system tenant)
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the stream was created (UTC).
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the model used for generation.
    /// </summary>
    public Model? ModelNavigation { get; set; }
}
