namespace Hartonomous.Core.Interfaces.Reasoning;

/// <summary>
/// Service for hydrating prompts with semantic context from the knowledge graph.
/// Retrieves atom content and spatial neighbors to provide enriched context for reasoning.
/// </summary>
public interface IContextRetrievalService
{
    /// <summary>
    /// Hydrates a prompt with context from the specified atoms and their semantic neighbors.
    /// </summary>
    /// <param name="prompt">The original user prompt.</param>
    /// <param name="contextAtomIds">The working set of atom IDs to include context from.</param>
    /// <param name="maxNeighbors">Maximum semantic neighbors to include per atom.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enhanced prompt with structured context.</returns>
    Task<HydratedContext> HydrateContextAsync(
        string prompt,
        IEnumerable<long> contextAtomIds,
        int maxNeighbors = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves canonical text and metadata for the specified atoms.
    /// </summary>
    /// <param name="atomIds">The atom IDs to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of atom context objects.</returns>
    Task<IEnumerable<AtomContext>> GetAtomContextsAsync(
        IEnumerable<long> atomIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds semantically similar atoms to the given embedding using spatial KNN.
    /// </summary>
    /// <param name="atomId">The reference atom ID.</param>
    /// <param name="k">Number of neighbors to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of neighbor atom IDs with similarity scores.</returns>
    Task<IEnumerable<SpatialNeighbor>> GetSpatialNeighborsAsync(
        long atomId,
        int k = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents hydrated context for LLM reasoning.
/// </summary>
public sealed class HydratedContext
{
    /// <summary>
    /// The original user prompt.
    /// </summary>
    public required string OriginalPrompt { get; set; }

    /// <summary>
    /// The enhanced system prompt with context.
    /// </summary>
    public required string SystemPrompt { get; set; }

    /// <summary>
    /// The atoms included in the context.
    /// </summary>
    public required List<AtomContext> ContextAtoms { get; set; }

    /// <summary>
    /// Semantic neighbors discovered through spatial queries.
    /// </summary>
    public required List<SpatialNeighbor> SemanticNeighbors { get; set; }

    /// <summary>
    /// Total tokens estimated for the context.
    /// </summary>
    public int EstimatedTokens { get; set; }

    /// <summary>
    /// Whether the context was truncated due to size limits.
    /// </summary>
    public bool WasTruncated { get; set; }
}

/// <summary>
/// Represents context data for a single atom.
/// </summary>
public sealed class AtomContext
{
    /// <summary>
    /// The atom ID.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// The canonical text representation.
    /// </summary>
    public required string CanonicalText { get; set; }

    /// <summary>
    /// The atom modality (text, code, image, etc.).
    /// </summary>
    public required string Modality { get; set; }

    /// <summary>
    /// The atom subtype.
    /// </summary>
    public string? Subtype { get; set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Relevance score to the query (0.0 to 1.0).
    /// </summary>
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Represents a spatial neighbor found via KNN.
/// </summary>
public sealed class SpatialNeighbor
{
    /// <summary>
    /// The neighbor atom ID.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// The Euclidean distance in embedding space.
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// Similarity score derived from distance (0.0 to 1.0).
    /// </summary>
    public double Similarity { get; set; }

    /// <summary>
    /// The canonical text of the neighbor.
    /// </summary>
    public string? CanonicalText { get; set; }
}
