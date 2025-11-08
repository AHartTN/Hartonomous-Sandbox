using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Operations;

public class DiagnosticRequest
{
    public string DiagnosticType { get; set; } = "slow_queries"; // slow_queries, blocking, deadlocks, resource_usage
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    [Range(1, 1000)]
    public int TopK { get; set; } = 10;
}
