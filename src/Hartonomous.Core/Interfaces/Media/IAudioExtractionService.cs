using Hartonomous.Core.Enums;

namespace Hartonomous.Core.Interfaces.Media;

/// <summary>
/// Service for extracting and processing audio from media files.
/// </summary>
public interface IAudioExtractionService
{
    /// <summary>
    /// Extracts audio from a video or audio file with filtering options.
    /// </summary>
    Task<bool> ExtractAudioAsync(
        string inputPath,
        string outputPath,
        AudioExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts audio waveform data for visualization.
    /// Returns amplitude values suitable for drawing waveforms.
    /// </summary>
    Task<double[]> ExtractWaveformAsync(
        string audioPath,
        int sampleCount = 1000,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for audio extraction from video/audio files.
/// </summary>
public class AudioExtractionOptions
{
    /// <summary>
    /// Start time for extraction (null = from beginning).
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Duration of audio to extract (null = until end).
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Output audio format.
    /// </summary>
    public MediaFormat OutputFormat { get; set; } = MediaFormat.MP3;

    /// <summary>
    /// Audio bitrate in kbps (e.g., 128, 192, 320).
    /// </summary>
    public int? AudioBitrate { get; set; } = 192;

    /// <summary>
    /// Sample rate in Hz (e.g., 44100, 48000).
    /// </summary>
    public int? SampleRate { get; set; }

    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public AudioChannel? Channels { get; set; }

    /// <summary>
    /// Strip video track (force audio-only output).
    /// </summary>
    public bool AudioOnly { get; set; } = true;
}
