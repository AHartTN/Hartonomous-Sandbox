namespace Hartonomous.Api.DTOs.Generation;

public sealed class AudioGenerationResponse
{
    public long InferenceId { get; set; }
    public long? AudioId { get; set; }
    public string? SourcePath { get; set; }
    public int DurationMs { get; set; }
    public int SampleRate { get; set; }
    public int NumChannels { get; set; }
    public double Score { get; set; }
    public string? SynthesizedAudioBase64 { get; set; }
    public string? SegmentPlan { get; set; }
}
