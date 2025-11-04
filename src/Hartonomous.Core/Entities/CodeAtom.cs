using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents an atomized code snippet for code generation via semantic search.
/// PRIMARY FOCUS: T-SQL procedures, CLR functions, optimizations.
/// Database-native code search: Query similar code via VECTOR embeddings.
/// </summary>
public class CodeAtom
{
    /// <summary>
    /// Unique identifier for the code atom.
    /// </summary>
    public long CodeAtomId { get; set; }

    /// <summary>
    /// Programming language (TSql, CSharp, FSharp, etc.).
    /// PRIMARY: "TSql" for T-SQL procedures and CLR functions.
    /// </summary>
    public string Language { get; set; } = "TSql";

    /// <summary>
    /// The actual code snippet (stored as TEXT for large procedures).
    /// Can be complete procedure, function, or reusable code fragment.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Framework/context for the code (e.g., "SQL Server 2025", ".NET 10 CLR", "EF Core 10").
    /// </summary>
    public string? Framework { get; set; }

    /// <summary>
    /// Semantic description of what the code does (used for search).
    /// Example: "Weighted ensemble inference with spatial projection"
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category/type of code (Procedure, Function, View, Optimization, CLR, etc.).
    /// </summary>
    public string? CodeType { get; set; }

    /// <summary>
    /// Vector embedding of the code (VECTOR(1998) in SQL Server 2025).
    /// Enables semantic search for similar code patterns.
    /// Stored as GEOMETRY LINESTRING ZM for compatibility.
    /// </summary>
    public Geometry? Embedding { get; set; }

    /// <summary>
    /// Embedding dimension (actual length of vector, max 1998).
    /// </summary>
    public int? EmbeddingDimension { get; set; }

    /// <summary>
    /// Test results as JSON (mapped to SQL Server 2025 JSON type).
    /// Stores execution results, performance metrics, validation status.
    /// Example: { "status": "passed", "duration_ms": 125, "rows_affected": 1000, "errors": [] }
    /// </summary>
    public string? TestResults { get; set; }

    /// <summary>
    /// Quality score (0.0-1.0) based on test results, performance, usage frequency.
    /// Higher scores = more trusted code for generation.
    /// </summary>
    public float? QualityScore { get; set; }

    /// <summary>
    /// Number of times this code atom has been used in generation.
    /// Tracks popularity for ranking.
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// SHA256 hash of the code for deduplication.
    /// </summary>
    public byte[]? CodeHash { get; set; }

    /// <summary>
    /// Original source URI (file path, git URL, documentation link).
    /// </summary>
    public string? SourceUri { get; set; }

    /// <summary>
    /// Tags for categorization (JSON array: ["optimization", "vector-search", "ensemble"]).
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Timestamp when the code atom was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the code atom was last updated (e.g., after testing).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User or system that created this code atom.
    /// </summary>
    public string? CreatedBy { get; set; }
}
