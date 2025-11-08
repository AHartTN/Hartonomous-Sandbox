namespace Hartonomous.Api.DTOs.Operations;

public class TopQueryEntry
{
    public long QueryId { get; set; }
    public required string QueryText { get; set; }
    public long ExecutionCount { get; set; }
    public double AvgDurationMs { get; set; }
    public double AvgCpuTimeMs { get; set; }
    public double AvgLogicalReads { get; set; }
    public DateTime? LastExecutionTime { get; set; }
}
