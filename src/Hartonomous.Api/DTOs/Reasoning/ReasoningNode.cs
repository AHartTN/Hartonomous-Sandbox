namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Single node in a Tree of Thought reasoning tree.
/// </summary>
public sealed class ReasoningNode
{
    /// <summary>
    /// Path identifier (which parallel path this node belongs to).
    /// </summary>
    public int PathId { get; set; }

    /// <summary>
    /// Step number within the path (depth level).
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Branch identifier (which branch at this step).
    /// </summary>
    public int BranchId { get; set; }

    /// <summary>
    /// Prompt used to generate this node.
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Generated response for this node.
    /// </summary>
    public required string Response { get; set; }

    /// <summary>
    /// Quality score for this reasoning path segment.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Timestamp when this node was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
