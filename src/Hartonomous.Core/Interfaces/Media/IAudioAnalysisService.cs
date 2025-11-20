using Hartonomous.Core.Models.Media;

namespace Hartonomous.Core.Interfaces.Media;

/// <summary>
/// Service for analyzing audio characteristics and frequency content.
/// </summary>
public interface IAudioAnalysisService
{
    /// <summary>
    /// Performs Fast Fourier Transform (FFT) on audio to extract frequency spectrum.
    /// Useful for spectrograms, frequency analysis, and audio visualization.
    /// </summary>
    Task<FrequencySpectrum> AnalyzeFrequencySpectrumAsync(
        string audioPath,
        TimeSpan? startTime = null,
        TimeSpan? duration = null,
        int fftSize = 2048,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a spectrogram (time-frequency heatmap) of an audio file.
    /// Returns 2D array where [time][frequency] = magnitude.
    /// </summary>
    Task<Spectrogram> GenerateSpectrogramAsync(
        string audioPath,
        int fftSize = 2048,
        int hopSize = 512,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes audio characteristics like RMS level, peak amplitude, zero crossing rate, etc.
    /// </summary>
    Task<AudioAnalysis> AnalyzeAudioAsync(
        string audioPath,
        CancellationToken cancellationToken = default);
}
