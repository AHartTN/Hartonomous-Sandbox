namespace Hartonomous.Core.Interfaces.Provenance;

/// <summary>
/// Represents a service that queries provenance data from Neo4j graph database.
/// Provides READ-ONLY analytical Cypher queries for lineage tracking and error analysis.
/// </summary>
public interface IProvenanceQueryService
{
    /// <summary>
    /// Gets the complete lineage (ancestry chain) for a specific atom.
    /// </summary>
    /// <param name="atomId">The atom ID to trace lineage for.</param>
    /// <param name="maxDepth">Maximum depth to traverse (default: unlimited).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The lineage tree showing all ancestor atoms and relationships.</returns>
    Task<AtomLineage> GetAtomLineageAsync(
        long atomId,
        int? maxDepth = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds clusters of related errors in the provenance graph.
    /// Useful for identifying systematic failures or recurring issues.
    /// </summary>
    /// <param name="sessionId">Optional session ID to scope the search.</param>
    /// <param name="minClusterSize">Minimum number of errors to constitute a cluster.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of error clusters with common patterns.</returns>
    Task<IEnumerable<ErrorCluster>> FindErrorClustersAsync(
        long? sessionId = null,
        int minClusterSize = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reasoning paths taken within a session.
    /// Shows the complete decision tree with branches explored.
    /// </summary>
    /// <param name="sessionId">The session ID to analyze.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of reasoning paths with decision points and outcomes.</returns>
    Task<IEnumerable<ReasoningPath>> GetSessionPathsAsync(
        long sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds atoms that influenced a specific conclusion or result.
    /// Traces backward through the provenance graph to identify contributory factors.
    /// </summary>
    /// <param name="resultAtomId">The result atom to trace influences for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of atoms that contributed to the result, with influence weights.</returns>
    Task<IEnumerable<AtomInfluence>> GetInfluencingAtomsAsync(
        long resultAtomId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the lineage (ancestry) of an atom in the provenance graph.
/// </summary>
public sealed class AtomLineage
{
    /// <summary>
    /// Gets or sets the root atom ID.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the parent atoms (immediate ancestors).
    /// </summary>
    public required List<AtomNode> Parents { get; set; }

    /// <summary>
    /// Gets or sets the total depth of the lineage tree.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Gets or sets the total number of ancestor atoms.
    /// </summary>
    public int TotalAncestors { get; set; }
}

/// <summary>
/// Represents a node in the provenance graph with relationship information.
/// </summary>
public sealed class AtomNode
{
    /// <summary>
    /// Gets or sets the atom ID.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the relationship type (e.g., "DERIVED_FROM", "INFLUENCED_BY").
    /// </summary>
    public required string RelationshipType { get; set; }

    /// <summary>
    /// Gets or sets the child nodes (descendants).
    /// </summary>
    public List<AtomNode>? Children { get; set; }

    /// <summary>
    /// Gets or sets metadata about the atom as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Represents a cluster of related errors in the provenance graph.
/// </summary>
public sealed class ErrorCluster
{
    /// <summary>
    /// Gets or sets the unique cluster identifier.
    /// </summary>
    public required string ClusterId { get; set; }

    /// <summary>
    /// Gets or sets the number of errors in the cluster.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Gets or sets the common error pattern or root cause.
    /// </summary>
    public required string Pattern { get; set; }

    /// <summary>
    /// Gets or sets the atom IDs that are part of this cluster.
    /// </summary>
    public required List<long> AtomIds { get; set; }

    /// <summary>
    /// Gets or sets the first occurrence timestamp.
    /// </summary>
    public DateTime FirstOccurrence { get; set; }

    /// <summary>
    /// Gets or sets the last occurrence timestamp.
    /// </summary>
    public DateTime LastOccurrence { get; set; }
}

/// <summary>
/// Represents a reasoning path taken during a session.
/// </summary>
public sealed class ReasoningPath
{
    /// <summary>
    /// Gets or sets the path identifier.
    /// </summary>
    public required string PathId { get; set; }

    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public long SessionId { get; set; }

    /// <summary>
    /// Gets or sets the sequence of atom IDs in the path.
    /// </summary>
    public required List<long> AtomSequence { get; set; }

    /// <summary>
    /// Gets or sets the decision points along the path.
    /// </summary>
    public required List<DecisionPoint> Decisions { get; set; }

    /// <summary>
    /// Gets or sets whether this path led to a successful outcome.
    /// </summary>
    public bool IsSuccessful { get; set; }
}

/// <summary>
/// Represents a decision point in a reasoning path.
/// </summary>
public sealed class DecisionPoint
{
    /// <summary>
    /// Gets or sets the atom ID where the decision was made.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the decision description.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the branches considered at this decision point.
    /// </summary>
    public List<string>? BranchesConsidered { get; set; }

    /// <summary>
    /// Gets or sets the branch that was chosen.
    /// </summary>
    public string? ChosenBranch { get; set; }
}

/// <summary>
/// Represents an atom that influenced a specific result.
/// </summary>
public sealed class AtomInfluence
{
    /// <summary>
    /// Gets or sets the influencing atom ID.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the influence weight (0.0 to 1.0).
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Gets or sets the type of influence (e.g., "Direct", "Indirect").
    /// </summary>
    public required string InfluenceType { get; set; }

    /// <summary>
    /// Gets or sets the path length from the influencing atom to the result.
    /// </summary>
    public int PathLength { get; set; }
}
