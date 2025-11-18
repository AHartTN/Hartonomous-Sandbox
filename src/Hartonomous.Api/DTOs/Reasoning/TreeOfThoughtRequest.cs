namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Request for Tree of Thought reasoning (multi-path exploration).
/// </summary>
public sealed class TreeOfThoughtRequest
{
    /// <summary>
    /// Unique identifier for the problem being reasoned about.
    /// </summary>
    public Guid? ProblemId { get; set; }

    /// <summary>
    /// Base prompt to start all reasoning paths.
    /// </summary>
    public required string BasePrompt { get; set; }

    /// <summary>
    /// Number of parallel reasoning paths to explore.
    /// Default: 3
    /// </summary>
    public int? NumPaths { get; set; }

    /// <summary>
    /// Maximum depth for each reasoning path.
    /// Default: 3
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Number of branches at each decision point.
    /// Default: 2
    /// </summary>
    public int? BranchingFactor { get; set; }

    /// <summary>
    /// Enable debug output in stored procedure.
    /// </summary>
    public bool? Debug { get; set; }
}
