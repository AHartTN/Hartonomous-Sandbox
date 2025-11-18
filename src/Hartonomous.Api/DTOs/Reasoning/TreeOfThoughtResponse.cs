namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Response from Tree of Thought reasoning.
/// </summary>
public sealed class TreeOfThoughtResponse
{
    /// <summary>
    /// Unique identifier for the problem.
    /// </summary>
    public required Guid ProblemId { get; set; }

    /// <summary>
    /// Complete reasoning tree with all paths and nodes.
    /// </summary>
    public required List<ReasoningNode> Tree { get; set; }

    /// <summary>
    /// ID of the path with highest average score.
    /// </summary>
    public int BestPathId { get; set; }

    /// <summary>
    /// Total number of distinct paths explored.
    /// </summary>
    public int TotalPaths { get; set; }

    /// <summary>
    /// Total number of nodes in the reasoning tree.
    /// </summary>
    public int TotalNodes { get; set; }
}
