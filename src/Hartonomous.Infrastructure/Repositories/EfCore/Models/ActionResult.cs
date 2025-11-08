namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Result of executing a single action
/// </summary>
public class ActionResult
{
    public Guid HypothesisId { get; set; }
    public string HypothesisType { get; set; } = string.Empty;
    public string ExecutedActions { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ActionStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public int ExecutionTimeMs { get; set; }
}
