namespace Hartonomous.Api.DTOs.Generation;

public class GenerateAudioRequest : GenerationRequestBase
{
    /// <summary>Target duration in milliseconds</summary>
    public int TargetDurationMs { get; init; } = 5000;

    /// <summary>Sample rate in Hz</summary>
    public int SampleRate { get; init; } = 44100;

    /// <summary>Temperature for sampling</summary>
    public double Temperature { get; init; } = 0.6;
}

/// <summary>
/// Request to generate video from a text prompt
/// </summary>
