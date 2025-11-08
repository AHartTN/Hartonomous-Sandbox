namespace Hartonomous.Api.DTOs.Autonomy;

/// <summary>
/// Request to trigger the OODA loop Analyze phase
/// </summary>
public class TriggerAnalysisRequest
{
    /// <summary>Tenant ID for multi-tenant isolation</summary>
    public required int TenantId { get; init; }
    
    /// <summary>Analysis scope: 'full', 'models', 'embeddings', 'performance'</summary>
    public required string AnalysisScope { get; init; } = "full";
    
    /// <summary>Lookback window in hours for observation data</summary>
    public int LookbackHours { get; init; } = 24;
}

/// <summary>
/// Response from Analyze phase
/// </summary>
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
public class HypothesisResponse
{
    public required Guid AnalysisId { get; init; }
    public required int HypothesesGenerated { get; init; }
    public required List<Hypothesis> Hypotheses { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

public class Hypothesis
{
    public required Guid HypothesisId { get; init; }
    public required string HypothesisType { get; init; }
    public required int Priority { get; init; }
    public required string Description { get; init; }
    public required Dictionary<string, object> ExpectedImpact { get; init; }
    public required List<string> RequiredActions { get; init; }
}

/// <summary>
/// Response from Act phase (monitoring only)
/// </summary>
public class ActionResponse
{
    public required Guid AnalysisId { get; init; }
    public required int ExecutedActions { get; init; }
    public required int QueuedActions { get; init; }
    public required int FailedActions { get; init; }
    public required List<ActionResult> Results { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

public class ActionResult
{
    public required Guid HypothesisId { get; init; }
    public required string HypothesisType { get; init; }
    public required string ActionStatus { get; init; }
    public required Dictionary<string, object> ExecutedActions { get; init; }
    public required int ExecutionTimeMs { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Response from Learn phase (monitoring only)
/// </summary>
public class LearningResponse
{
    public required Guid AnalysisId { get; init; }
    public required bool LearningCycleComplete { get; init; }
    public required PerformanceMetrics PerformanceMetrics { get; init; }
    public required ActionOutcomeSummary ActionOutcomes { get; init; }
    public required List<ActionOutcome> Outcomes { get; init; }
    public required DateTime TimestampUtc { get; init; }
}

public class PerformanceMetrics
{
    public required double BaselineLatencyMs { get; init; }
    public required double CurrentLatencyMs { get; init; }
    public required double LatencyImprovement { get; init; }
    public required double ThroughputChange { get; init; }
}

public class ActionOutcomeSummary
{
    public required int SuccessfulActions { get; init; }
    public required int RegressedActions { get; init; }
    public required int TotalActions { get; init; }
}

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
public class QueueStatusResponse
{
    public required string QueueName { get; init; }
    public required int MessageCount { get; init; }
    public required int ConversationCount { get; init; }
    public required DateTime? LastMessageUtc { get; init; }
}

/// <summary>
/// OODA loop cycle history
/// </summary>
public class OodaCycleHistoryResponse
{
    public required List<OodaCycleRecord> Cycles { get; init; }
    public required int TotalCycles { get; init; }
    public required double AvgLatencyImprovement { get; init; }
}

public class OodaCycleRecord
{
    public required Guid AnalysisId { get; init; }
    public required DateTime StartTimeUtc { get; init; }
    public required DateTime? EndTimeUtc { get; init; }
    public required int HypothesesGenerated { get; init; }
    public required int ActionsExecuted { get; init; }
    public required double? LatencyImprovement { get; init; }
    public required string Status { get; init; }
}
