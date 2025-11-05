namespace Hartonomous.Api.DTOs.Inference;

public sealed class JobSubmittedResponse
{
    public long JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusUrl { get; set; } = string.Empty;
}
