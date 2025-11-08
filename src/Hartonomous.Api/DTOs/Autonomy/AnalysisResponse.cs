namespace Hartonomous.Api.DTOs.Autonomy;

public class AnalysisResponse
{
    public required Guid AnalysisId { get; init; }
    public required string AnalysisScope { get; init; }
    public required int TotalInferences { get; init; }
    public required double AvgDurationMs { get; init; }
    public required int AnomalyCount { get; init; }
    public required int PatternCount { get; init; }
    public required string Observations { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

/// <summary>
/// Response from Hypothesize phase (monitoring only)
/// </summary>
