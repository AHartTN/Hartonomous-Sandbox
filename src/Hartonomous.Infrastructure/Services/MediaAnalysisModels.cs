using System;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Represents frequency spectrum data from FFT analysis.
/// </summary>
public class FrequencySpectrum
{
    /// <summary>
    /// Frequency values in Hz for each bin.
    /// </summary>
    public required double[] Frequencies { get; init; }

    /// <summary>
    /// Magnitude values for each frequency bin.
    /// </summary>
    public required double[] Magnitudes { get; init; }

    /// <summary>
    /// Magnitude values in decibels (dB).
    /// </summary>
    public required double[] Decibels { get; init; }

    /// <summary>
    /// Sample rate used for analysis.
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// FFT window size used.
    /// </summary>
    public required int FftSize { get; init; }

    /// <summary>
    /// Frequency resolution (Hz per bin).
    /// </summary>
    public required double FrequencyResolution { get; init; }
}

/// <summary>
/// Represents a time-frequency spectrogram.
/// </summary>
public class Spectrogram
{
    /// <summary>
    /// 2D array of spectrogram data [timeFrame][frequencyBin] in dB.
    /// </summary>
    public required double[][] Data { get; init; }

    /// <summary>
    /// Number of time frames.
    /// </summary>
    public required int TimeFrames { get; init; }

    /// <summary>
    /// Number of frequency bins.
    /// </summary>
    public required int FrequencyBins { get; init; }

    /// <summary>
    /// Sample rate used.
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// FFT window size.
    /// </summary>
    public required int FftSize { get; init; }

    /// <summary>
    /// Hop size (samples between frames).
    /// </summary>
    public required int HopSize { get; init; }

    /// <summary>
    /// Frequency resolution (Hz per bin).
    /// </summary>
    public required double FrequencyResolution { get; init; }

    /// <summary>
    /// Time resolution (seconds per frame).
    /// </summary>
    public required double TimeResolution { get; init; }
}

/// <summary>
/// Comprehensive audio analysis results.
/// </summary>
public class AudioAnalysis
{
    /// <summary>
    /// Duration of the audio.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Number of channels.
    /// </summary>
    public required int Channels { get; init; }

    /// <summary>
    /// Bit depth.
    /// </summary>
    public required int BitDepth { get; init; }

    /// <summary>
    /// Audio codec name.
    /// </summary>
    public required string Codec { get; init; }

    /// <summary>
    /// Bitrate in bits per second.
    /// </summary>
    public required long Bitrate { get; init; }

    /// <summary>
    /// RMS (root mean square) level (0.0 to 1.0).
    /// </summary>
    public required double RmsLevel { get; init; }

    /// <summary>
    /// RMS level in decibels.
    /// </summary>
    public required double RmsLevelDb { get; init; }

    /// <summary>
    /// Peak level (0.0 to 1.0).
    /// </summary>
    public required double PeakLevel { get; init; }

    /// <summary>
    /// Peak level in decibels.
    /// </summary>
    public required double PeakLevelDb { get; init; }

    /// <summary>
    /// Zero crossing rate (indicator of noise/brightness).
    /// </summary>
    public required double ZeroCrossingRate { get; init; }

    /// <summary>
    /// Dynamic range (peak - RMS) in dB.
    /// </summary>
    public required double DynamicRange { get; init; }

    /// <summary>
    /// Total number of samples analyzed.
    /// </summary>
    public required int TotalSamples { get; init; }
}
