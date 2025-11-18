namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Response from Chain of Thought reasoning.
/// </summary>
public sealed class ChainOfThoughtResponse
{
    /// <summary>
    /// Unique identifier for the problem.
    /// </summary>
    public required Guid ProblemId { get; set; }

    /// <summary>
    /// Reasoning steps in sequence.
    /// </summary>
    public required List<ReasoningStep> Steps { get; set; }

    /// <summary>
    /// CLR aggregate analysis of reasoning chain coherence.
    /// JSON string containing coherence metrics.
    /// </summary>
    public string? CoherenceAnalysis { get; set; }

    /// <summary>
    /// Total number of reasoning steps generated.
    /// </summary>
    public int TotalSteps { get; set; }
}
