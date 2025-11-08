namespace Hartonomous.Api.DTOs.Operations;

public class DiagnosticEntry
{
    public required string Category { get; set; }
    public required string Description { get; set; }
    public DateTime? Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Query { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
}
