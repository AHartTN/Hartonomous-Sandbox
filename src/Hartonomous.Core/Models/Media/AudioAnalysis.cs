namespace Hartonomous.Core.Models.Media;

/// <summary>
/// Represents comprehensive audio analysis results.
/// </summary>
public class AudioAnalysis
{
    /// <summary>
    /// Duration of the audio file.
    /// </summary>
    public required TimeSpan Duration { get; set; }

    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public required int SampleRate { get; set; }

    /// <summary>
    /// Number of audio channels.
    /// </summary>
    public required int Channels { get; set; }

    /// <summary>
    /// Bit depth (e.g., 16, 24, 32).
    /// </summary>
    public required int BitDepth { get; set; }

    /// <summary>
    /// Audio codec name.
    /// </summary>
    public required string Codec { get; set; }

    /// <summary>
    /// Bitrate in bits per second.
    /// </summary>
    public required long Bitrate { get; set; }

    /// <summary>
    /// RMS (Root Mean Square) level (0.0 to 1.0).
    /// </summary>
    public required double RmsLevel { get; set; }

    /// <summary>
    /// RMS level in decibels.
    /// </summary>
    public required double RmsLevelDb { get; set; }

    /// <summary>
    /// Peak level (0.0 to 1.0).
    /// </summary>
    public required double PeakLevel { get; set; }

    /// <summary>
    /// Peak level in decibels.
    /// </summary>
    public required double PeakLevelDb { get; set; }

    /// <summary>
    /// Zero crossing rate (frequency of sign changes).
    /// </summary>
    public required double ZeroCrossingRate { get; set; }

    /// <summary>
    /// Dynamic range (difference between peak and RMS in dB).
    /// </summary>
    public required double DynamicRange { get; set; }

    /// <summary>
    /// Total number of audio samples.
    /// </summary>
    public required int TotalSamples { get; set; }
}
