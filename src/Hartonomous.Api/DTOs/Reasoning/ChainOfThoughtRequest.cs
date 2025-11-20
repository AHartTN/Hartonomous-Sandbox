namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Request model for Chain of Thought reasoning.
/// </summary>
public record ChainOfThoughtRequest
{
    /// <summary>
    /// The prompt to reason about.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Optional session ID for tracking related reasoning operations.
    /// If not provided, a new session will be created.
    /// </summary>
    public long SessionId { get; init; }
}
