namespace Hartonomous.Api.Controllers;

/// <summary>
/// Request model for Tree of Thought reasoning.
/// </summary>
public record TreeOfThoughtRequest
{
    /// <summary>
    /// The prompt to reason about.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Maximum number of reasoning branches to explore.
    /// Higher values provide more thorough exploration but take longer.
    /// </summary>
    public int MaxBranches { get; init; } = 3;

    /// <summary>
    /// Optional session ID for tracking related reasoning operations.
    /// If not provided, a new session will be created.
    /// </summary>
    public long SessionId { get; init; }
}
