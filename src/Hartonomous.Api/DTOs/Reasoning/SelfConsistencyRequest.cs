namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Request for Self-Consistency reasoning (consensus via multiple samples).
/// </summary>
public sealed class SelfConsistencyRequest
{
    /// <summary>
    /// Unique identifier for the problem being reasoned about.
    /// </summary>
    public Guid? ProblemId { get; set; }

    /// <summary>
    /// Prompt to generate multiple independent reasoning samples.
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Number of independent samples to generate.
    /// Default: 5
    /// </summary>
    public int? NumSamples { get; set; }

    /// <summary>
    /// Temperature for text generation (higher = more diversity).
    /// Default: 0.8
    /// Range: 0.0 to 1.0
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Enable debug output in stored procedure.
    /// </summary>
    public bool? Debug { get; set; }
}
