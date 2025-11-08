namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Metrics for action execution
/// </summary>
public class ExecutionMetrics
{
    public int TotalActions { get; set; }
    public int SuccessfulActions { get; set; }
    public int FailedActions { get; set; }
    public double AvgDurationMs { get; set; }
    public DateTime ExecutionStart { get; set; }
    public DateTime ExecutionEnd { get; set; }
}
