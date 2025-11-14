using System;
using System.Collections.Generic;
using Hartonomous.Data.Entities;
using Hartonomous.Core.Models;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Service abstraction for deduplicated atom ingestion.
/// </summary>
public interface IAtomIngestionService
{
    Task<AtomIngestionResult> IngestAsync(AtomIngestionRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request payload for atom ingestion.
/// </summary>
public class AtomIngestionRequest
{
    /// <summary>
    /// Content used to compute the SHA256 deduplication fingerprint.
    /// </summary>
    public required string HashInput { get; init; }

    /// <summary>
    /// Primary modality for the atom (text, code, image_patch, audio_frame, etc.).
    /// </summary>
    public required string Modality { get; init; }

    public string? Subtype { get; init; }

    public string? SourceUri { get; init; }

    public string? SourceType { get; init; }

    public string? CanonicalText { get; init; }

    public string? Metadata { get; init; }

    public string? PayloadLocator { get; init; }

    /// <summary>
    /// Optional embedding for this atom. When provided, dimensionality and spatial coordinates are stored.
    /// </summary>
    public float[]? Embedding { get; init; }

    /// <summary>
    /// Logical label for the embedding (e.g. text-embedding-3-large).
    /// </summary>
    public string EmbeddingType { get; init; } = "default";

    /// <summary>
    /// Associated model identifier when the embedding comes from an internal model.
    /// </summary>
    public int? ModelId { get; init; }

    /// <summary>
    /// Override to use a non-default deduplication policy.
    /// </summary>
    public string PolicyName { get; init; } = "default";

    /// <summary>
    /// Ordered component descriptors representing the aggregated makeup of the atom.
    /// </summary>
    public IReadOnlyList<AtomComponentDescriptor> Components { get; init; } = Array.Empty<AtomComponentDescriptor>();
}

/// <summary>
/// Result of atom ingestion.
/// </summary>
public class AtomIngestionResult
{
    public required Atom Atom { get; init; }

    public AtomEmbedding? Embedding { get; init; }

    public bool WasDuplicate { get; init; }

    public string? DuplicateReason { get; init; }

    public double? SemanticSimilarity { get; init; }
}
