using Hartonomous.Data.Entities;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// Strongly-typed data models for AtomIngestion pipeline steps.
/// These replace tuples with enterprise-grade DTOs for better maintainability,
/// type safety, and extensibility.
/// </summary>

/// <summary>
/// Output from hash computation step.
/// Contains original request and computed content hash.
/// </summary>
public sealed record HashComputationResult
{
    public required AtomIngestionPipelineRequest Request { get; init; }
    public required byte[] ContentHash { get; init; }
}

/// <summary>
/// Output from exact duplicate check step.
/// Contains hash result and any existing atom found by content hash.
/// </summary>
public sealed record ExactDuplicateCheckResult
{
    public required AtomIngestionPipelineRequest Request { get; init; }
    public required byte[] ContentHash { get; init; }
    public Atom? ExistingAtom { get; init; }
    public bool IsDuplicate => ExistingAtom != null;
}

/// <summary>
/// Output from embedding generation step.
/// Contains duplicate check result and generated embedding vector.
/// </summary>
public sealed record EmbeddingGenerationResult
{
    public required AtomIngestionPipelineRequest Request { get; init; }
    public required byte[] ContentHash { get; init; }
    public Atom? ExistingAtom { get; init; }
    public required float[] EmbeddingVector { get; init; }
}

/// <summary>
/// Output from semantic duplicate check step.
/// Contains embedding result and any semantically similar atom found.
/// </summary>
public sealed record SemanticDuplicateCheckResult
{
    public required AtomIngestionPipelineRequest Request { get; init; }
    public required byte[] ContentHash { get; init; }
    public Atom? ExistingAtom { get; init; }
    public required float[] EmbeddingVector { get; init; }
    public Atom? SimilarAtom { get; init; }
    public float? SimilarityScore { get; init; }
    public bool IsDuplicate => SimilarAtom != null;
}

/// <summary>
/// Output from atom creation step.
/// Contains the newly created (or reused) atom entity.
/// </summary>
public sealed record AtomCreationResult
{
    public required Atom Atom { get; init; }
    public required byte[] ContentHash { get; init; }
    public required float[] EmbeddingVector { get; init; }
    public bool WasNewlyCreated { get; init; }
    public string? ReuseReason { get; init; }
}

/// <summary>
/// Output from Neo4j sync step.
/// Contains atom and graph sync status.
/// </summary>
public sealed record Neo4jSyncResult
{
    public required Atom Atom { get; init; }
    public required byte[] ContentHash { get; init; }
    public required float[] EmbeddingVector { get; init; }
    public bool WasSynced { get; init; }
    public string? SyncError { get; init; }
}
