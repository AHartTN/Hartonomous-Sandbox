namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Response from Self-Consistency reasoning.
/// </summary>
public sealed class SelfConsistencyResponse
{
    /// <summary>
    /// Unique identifier for the problem.
    /// </summary>
    public required Guid ProblemId { get; set; }

    /// <summary>
    /// Consensus answer extracted via CLR aggregate analysis.
    /// </summary>
    public string? ConsensusAnswer { get; set; }

    /// <summary>
    /// Ratio of samples agreeing with consensus (0.0 to 1.0).
    /// Higher = stronger agreement.
    /// </summary>
    public double AgreementRatio { get; set; }

    /// <summary>
    /// Number of samples supporting the consensus answer.
    /// </summary>
    public int NumSupportingSamples { get; set; }

    /// <summary>
    /// Average confidence across all samples.
    /// </summary>
    public double AvgConfidence { get; set; }

    /// <summary>
    /// All generated samples.
    /// </summary>
    public required List<ReasoningSample> Samples { get; set; }

    /// <summary>
    /// CLR aggregate consensus analysis (JSON string).
    /// </summary>
    public string? ConsensusMetrics { get; set; }
}
