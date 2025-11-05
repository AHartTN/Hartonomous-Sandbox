namespace Hartonomous.Api.DTOs.Inference;

public sealed class JobStatusResponse
{
    public long JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TaskType { get; set; }
    public string? OutputData { get; set; }
    public double? Confidence { get; set; }
    public int? DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
