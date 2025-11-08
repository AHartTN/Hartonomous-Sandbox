namespace Hartonomous.Api.DTOs.Generation;

public class GenerationJobStatus
{
    public required long JobId { get; init; }
    public required string Status { get; init; }
    public required string ContentType { get; init; }
    public int ProgressPercent { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ResultUrl { get; init; }
    public string? ErrorMessage { get; init; }
}
