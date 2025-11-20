namespace Hartonomous.Core.Models.Media;

/// <summary>
/// Represents frequency spectrum data from FFT analysis.
/// </summary>
public class FrequencySpectrum
{
    /// <summary>
    /// Frequency values in Hz for each bin.
    /// </summary>
    public required double[] Frequencies { get; set; }

    /// <summary>
    /// Magnitude values for each frequency bin.
    /// </summary>
    public required double[] Magnitudes { get; set; }

    /// <summary>
    /// Magnitude values in decibels for each frequency bin.
    /// </summary>
    public required double[] Decibels { get; set; }

    /// <summary>
    /// Sample rate of the audio in Hz.
    /// </summary>
    public required int SampleRate { get; set; }

    /// <summary>
    /// FFT window size used for analysis.
    /// </summary>
    public required int FftSize { get; set; }

    /// <summary>
    /// Frequency resolution (Hz per bin).
    /// </summary>
    public required double FrequencyResolution { get; set; }
}
