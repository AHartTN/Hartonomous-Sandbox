namespace Hartonomous.Api.DTOs.Autonomy;

public class ActionOutcome
{
    public required Guid HypothesisId { get; init; }
    public required string HypothesisType { get; init; }
    public required string ActionStatus { get; init; }
    public required string Outcome { get; init; }
    public required double ImpactScore { get; init; }
}

/// <summary>
/// Service Broker queue status for monitoring
/// </summary>
