namespace Hartonomous.Core.Models.Media;

/// <summary>
/// Represents a spectrogram (time-frequency representation of audio).
/// </summary>
public class Spectrogram
{
    /// <summary>
    /// 2D array where [time][frequency] = magnitude in dB.
    /// </summary>
    public required double[][] Data { get; set; }

    /// <summary>
    /// Number of time frames in the spectrogram.
    /// </summary>
    public required int TimeFrames { get; set; }

    /// <summary>
    /// Number of frequency bins in the spectrogram.
    /// </summary>
    public required int FrequencyBins { get; set; }

    /// <summary>
    /// Sample rate of the audio in Hz.
    /// </summary>
    public required int SampleRate { get; set; }

    /// <summary>
    /// FFT window size used for analysis.
    /// </summary>
    public required int FftSize { get; set; }

    /// <summary>
    /// Hop size (number of samples between consecutive FFT windows).
    /// </summary>
    public required int HopSize { get; set; }

    /// <summary>
    /// Frequency resolution (Hz per bin).
    /// </summary>
    public required double FrequencyResolution { get; set; }

    /// <summary>
    /// Time resolution (seconds per frame).
    /// </summary>
    public required double TimeResolution { get; set; }
}
