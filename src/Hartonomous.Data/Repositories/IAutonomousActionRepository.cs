using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using NetTopologySuite.Geometries;

namespace Hartonomous.Data.Repositories;

/// <summary>
/// Interface for autonomous action operations
/// Replaces sp_Act stored procedure
/// </summary>
public interface IAutonomousActionRepository
{
    /// <summary>
    /// Executes actions based on hypotheses received from analysis phase
    /// </summary>
    Task<ActionExecutionResult> ExecuteActionsAsync(
        Guid analysisId,
        IReadOnlyList<Hypothesis> hypotheses,
        int autoApproveThreshold = 3,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a hypothesis generated for system improvement
/// </summary>
public class Hypothesis
{
    public Guid HypothesisId { get; set; }
    public string HypothesisType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> RequiredActions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Result of action execution
/// </summary>
public class ActionExecutionResult
{
    public Guid AnalysisId { get; set; }
    public int ExecutedActions { get; set; }
    public int QueuedActions { get; set; }
    public int FailedActions { get; set; }
    public IReadOnlyList<ActionResult> Results { get; set; } = Array.Empty<ActionResult>();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Result of executing a specific action
/// </summary>
public class ActionResult
{
    public Guid HypothesisId { get; set; }
    public string HypothesisType { get; set; } = string.Empty;
    public string ExecutedActions { get; set; } = string.Empty;
    public string ActionStatus { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}