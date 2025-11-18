namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Single step in a reasoning chain.
/// </summary>
public sealed class ReasoningStep
{
    /// <summary>
    /// Step number in the sequence (1-indexed).
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Prompt used to generate this step.
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Generated response for this step.
    /// </summary>
    public required string Response { get; set; }

    /// <summary>
    /// Confidence score for this step (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Timestamp when this step was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
