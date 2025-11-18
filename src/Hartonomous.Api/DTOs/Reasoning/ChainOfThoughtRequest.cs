namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Request for Chain of Thought reasoning.
/// </summary>
public sealed class ChainOfThoughtRequest
{
    /// <summary>
    /// Unique identifier for the problem being reasoned about.
    /// </summary>
    public Guid? ProblemId { get; set; }

    /// <summary>
    /// Initial prompt to start the reasoning chain.
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Maximum number of reasoning steps to generate.
    /// Default: 5
    /// </summary>
    public int? MaxSteps { get; set; }

    /// <summary>
    /// Temperature for text generation (higher = more creative).
    /// Default: 0.7
    /// Range: 0.0 to 1.0
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Enable debug output in stored procedure.
    /// </summary>
    public bool? Debug { get; set; }
}
