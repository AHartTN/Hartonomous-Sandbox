namespace Hartonomous.Core.Models.Audio;

/// <summary>
/// Buffer of audio samples from a stream.
/// </summary>
public class AudioBuffer
{
    public required string StreamId { get; set; }
    public required string BufferId { get; set; }
    public required int SampleRate { get; set; } // Samples per second (Hz)
    public required int Channels { get; set; } // 1=mono, 2=stereo, etc.
    public required int BitsPerSample { get; set; } // 16, 24, 32
    public required int SampleCount { get; set; } // Number of samples (not bytes)
    public required DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Raw PCM audio data.
    /// Length must be SampleCount * Channels * (BitsPerSample / 8).
    /// </summary>
    public required byte[] Samples { get; set; }
}
