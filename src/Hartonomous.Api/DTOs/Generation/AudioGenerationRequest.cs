namespace Hartonomous.Api.DTOs.Generation;

public sealed class AudioGenerationRequest
{
    public required string Prompt { get; set; }
    public int? TargetDurationMs { get; set; }
    public int? SampleRate { get; set; }
    public int? TopK { get; set; }
    public double? Temperature { get; set; }
    public string? ModelIds { get; set; }
}
