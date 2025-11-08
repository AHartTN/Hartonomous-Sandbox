using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;

namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

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
