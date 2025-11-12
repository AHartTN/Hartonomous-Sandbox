using System;
using System.Collections.Generic;
using System.Linq;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// Audio feature extraction result containing time-domain and frequency-domain features
/// </summary>
public record AudioFeatures
{
    /// <summary>
    /// Zero-crossing rate: count of sign changes per sample (correlates with frequency content)
    /// </summary>
    public double ZeroCrossingRate { get; init; }

    /// <summary>
    /// Spectral centroid: weighted mean frequency (Hz) - perceived "brightness" of sound
    /// </summary>
    public double SpectralCentroid { get; init; }

    /// <summary>
    /// Spectral flatness: geometric mean / arithmetic mean of spectrum (0=tonal, 1=noise-like)
    /// </summary>
    public double SpectralFlatness { get; init; }

    /// <summary>
    /// Spectral rolloff: frequency (Hz) below which 85% of spectral energy is concentrated
    /// </summary>
    public double SpectralRolloff { get; init; }

    /// <summary>
    /// Spectral bandwidth: standard deviation of frequencies around spectral centroid
    /// </summary>
    public double SpectralBandwidth { get; init; }

    /// <summary>
    /// Estimated tempo in beats per minute (BPM)
    /// </summary>
    public double Tempo { get; init; }

    /// <summary>
    /// Prominent tone frequencies in different bands
    /// </summary>
    public ProminentTones Tones { get; init; } = new();
}

/// <summary>
/// Prominent tone frequencies in bass, mid, and treble bands
/// </summary>
public record ProminentTones
{
    /// <summary>
    /// Dominant frequency in bass range (20-250 Hz)
    /// </summary>
    public double BassFrequency { get; init; }

    /// <summary>
    /// Dominant frequency in mid range (250-4000 Hz)
    /// </summary>
    public double MidFrequency { get; init; }

    /// <summary>
    /// Dominant frequency in treble range (4000-20000 Hz)
    /// </summary>
    public double TrebleFrequency { get; init; }
}

/// <summary>
/// Internal audio feature extraction algorithms using DSP techniques.
/// Calculates time-domain and frequency-domain features for audio quality assessment and semantic understanding.
/// </summary>
public static class AudioFeatureExtractor
{
    private const int BassBandMin = 20;      // Hz
    private const int BassBandMax = 250;     // Hz
    private const int MidBandMin = 250;      // Hz
    private const int MidBandMax = 4000;     // Hz
    private const int TrebleBandMin = 4000;  // Hz
    private const int TrebleBandMax = 20000; // Hz

    /// <summary>
    /// Extract comprehensive audio features from float samples
    /// </summary>
    /// <param name="samples">Audio samples (normalized -1.0 to 1.0)</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="fftSize">FFT window size for spectral analysis (default 2048)</param>
    /// <returns>Extracted audio features</returns>
    public static AudioFeatures ExtractFeatures(
        float[] samples, 
        int sampleRate, 
        int fftSize = 2048)
    {
        if (samples == null || samples.Length == 0)
        {
            throw new ArgumentException("Samples cannot be null or empty", nameof(samples));
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentException("Sample rate must be positive", nameof(sampleRate));
        }

        // Time-domain feature: Zero-crossing rate
        double zcr = CalculateZeroCrossingRate(samples);

        // Frequency-domain features require FFT
        // Compute magnitude spectrum from first window
        int windowSize = Math.Min(fftSize, samples.Length);
        float[] window = new float[windowSize];
        Array.Copy(samples, 0, window, 0, windowSize);

        // Apply Hann window to reduce spectral leakage
        ApplyHannWindow(window);

        // Convert float to short for FftReal (expects 16-bit PCM)
        var shortSamples = new short[windowSize];
        for (int i = 0; i < windowSize; i++)
        {
            shortSamples[i] = (short)(window[i] * 32767.0f);
        }

        // Compute FFT and get magnitude spectrum
        var fftOutput = FftProcessor.FftReal(shortSamples);
        double[] magnitudes = FftProcessor.MagnitudeSpectrum(fftOutput)
            .Take(windowSize / 2) // Only first half (Nyquist theorem)
            .ToArray();

        // Calculate frequency bin resolution
        double freqResolution = (double)sampleRate / windowSize;

        // Spectral features
        double centroid = CalculateSpectralCentroid(magnitudes, freqResolution);
        double flatness = CalculateSpectralFlatness(magnitudes);
        double rolloff = CalculateSpectralRolloff(magnitudes, freqResolution);
        double bandwidth = CalculateSpectralBandwidth(magnitudes, freqResolution, centroid);

        // Prominent tones in frequency bands
        var tones = ExtractProminentTones(magnitudes, freqResolution);

        // Tempo detection (simple onset-based approach)
        double tempo = EstimateTempo(samples, sampleRate);

        return new AudioFeatures
        {
            ZeroCrossingRate = zcr,
            SpectralCentroid = centroid,
            SpectralFlatness = flatness,
            SpectralRolloff = rolloff,
            SpectralBandwidth = bandwidth,
            Tempo = tempo,
            Tones = tones
        };
    }

    /// <summary>
    /// Calculate zero-crossing rate: count of sign changes per sample
    /// Higher ZCR correlates with higher frequency content (noisier signals)
    /// </summary>
    private static double CalculateZeroCrossingRate(float[] samples)
    {
        int crossings = 0;
        for (int i = 1; i < samples.Length; i++)
        {
            // Sign change detected: previous and current have opposite signs
            if ((samples[i - 1] >= 0 && samples[i] < 0) ||
                (samples[i - 1] < 0 && samples[i] >= 0))
            {
                crossings++;
            }
        }

        return (double)crossings / samples.Length;
    }

    /// <summary>
    /// Calculate spectral centroid: weighted mean frequency
    /// Formula: Σ(freq[i] * magnitude[i]) / Σ(magnitude[i])
    /// Indicates perceived "brightness" - higher centroid = brighter sound
    /// </summary>
    private static double CalculateSpectralCentroid(double[] magnitudes, double freqResolution)
    {
        double weightedSum = 0.0;
        double totalMagnitude = 0.0;

        for (int i = 0; i < magnitudes.Length; i++)
        {
            double frequency = i * freqResolution;
            weightedSum += frequency * magnitudes[i];
            totalMagnitude += magnitudes[i];
        }

        return totalMagnitude > 0 ? weightedSum / totalMagnitude : 0.0;
    }

    /// <summary>
    /// Calculate spectral flatness: geometric mean / arithmetic mean
    /// Value range: 0 (pure tone) to 1 (white noise)
    /// Formula: exp(mean(log(mag))) / mean(mag)
    /// </summary>
    private static double CalculateSpectralFlatness(double[] magnitudes)
    {
        // Avoid log(0) by adding small epsilon
        const double epsilon = 1e-10;

        double geometricMean = 0.0;
        double arithmeticMean = 0.0;
        int count = 0;

        foreach (var mag in magnitudes)
        {
            if (mag > epsilon)
            {
                geometricMean += Math.Log(mag + epsilon);
                arithmeticMean += mag;
                count++;
            }
        }

        if (count == 0)
            return 0.0;

        geometricMean = Math.Exp(geometricMean / count);
        arithmeticMean /= count;

        return arithmeticMean > epsilon ? geometricMean / arithmeticMean : 0.0;
    }

    /// <summary>
    /// Calculate spectral rolloff: frequency below which 85% of energy is concentrated
    /// Indicates frequency extent - higher rolloff = more high-frequency content
    /// </summary>
    private static double CalculateSpectralRolloff(double[] magnitudes, double freqResolution, double threshold = 0.85)
    {
        // Calculate total energy
        double totalEnergy = magnitudes.Sum();
        double thresholdEnergy = threshold * totalEnergy;

        // Find frequency where cumulative energy reaches threshold
        double cumulativeEnergy = 0.0;
        for (int i = 0; i < magnitudes.Length; i++)
        {
            cumulativeEnergy += magnitudes[i];
            if (cumulativeEnergy >= thresholdEnergy)
            {
                return i * freqResolution;
            }
        }

        return (magnitudes.Length - 1) * freqResolution;
    }

    /// <summary>
    /// Calculate spectral bandwidth: standard deviation of frequencies around centroid
    /// Formula: sqrt(Σ((freq - centroid)² * mag) / Σ(mag))
    /// Indicates spread of frequency distribution
    /// </summary>
    private static double CalculateSpectralBandwidth(
        double[] magnitudes, 
        double freqResolution, 
        double centroid)
    {
        double varianceSum = 0.0;
        double totalMagnitude = 0.0;

        for (int i = 0; i < magnitudes.Length; i++)
        {
            double frequency = i * freqResolution;
            double deviation = frequency - centroid;
            varianceSum += deviation * deviation * magnitudes[i];
            totalMagnitude += magnitudes[i];
        }

        if (totalMagnitude <= 0)
            return 0.0;

        double variance = varianceSum / totalMagnitude;
        return Math.Sqrt(variance);
    }

    /// <summary>
    /// Extract prominent tone frequencies in bass, mid, and treble bands
    /// Finds peak magnitude in each frequency band
    /// </summary>
    private static ProminentTones ExtractProminentTones(double[] magnitudes, double freqResolution)
    {
        // Find peak in each band
        double bassPeak = FindPeakInBand(magnitudes, freqResolution, BassBandMin, BassBandMax);
        double midPeak = FindPeakInBand(magnitudes, freqResolution, MidBandMin, MidBandMax);
        double treblePeak = FindPeakInBand(magnitudes, freqResolution, TrebleBandMin, TrebleBandMax);

        return new ProminentTones
        {
            BassFrequency = bassPeak,
            MidFrequency = midPeak,
            TrebleFrequency = treblePeak
        };
    }

    /// <summary>
    /// Find frequency with maximum magnitude in specified band
    /// </summary>
    private static double FindPeakInBand(
        double[] magnitudes, 
        double freqResolution, 
        int minFreq, 
        int maxFreq)
    {
        int startBin = (int)(minFreq / freqResolution);
        int endBin = Math.Min((int)(maxFreq / freqResolution), magnitudes.Length - 1);

        if (startBin >= magnitudes.Length || endBin <= startBin)
            return 0.0;

        double maxMagnitude = 0.0;
        int peakBin = startBin;

        for (int i = startBin; i <= endBin; i++)
        {
            if (magnitudes[i] > maxMagnitude)
            {
                maxMagnitude = magnitudes[i];
                peakBin = i;
            }
        }

        return peakBin * freqResolution;
    }

    /// <summary>
    /// Estimate tempo (BPM) using simple onset detection and autocorrelation
    /// This is a basic implementation - more sophisticated methods exist
    /// </summary>
    private static double EstimateTempo(float[] samples, int sampleRate)
    {
        // Tempo detection requires longer audio (at least a few seconds)
        if (samples.Length < sampleRate * 2)
            return 0.0; // Not enough data

        // Simple energy-based onset detection
        const int hopSize = 512; // ~11ms at 44.1kHz
        var onsetStrength = new List<double>();

        for (int i = 0; i < samples.Length - hopSize; i += hopSize)
        {
            // Calculate RMS energy for this frame
            double energy = 0.0;
            for (int j = 0; j < hopSize; j++)
            {
                energy += samples[i + j] * samples[i + j];
            }
            energy = Math.Sqrt(energy / hopSize);
            onsetStrength.Add(energy);
        }

        if (onsetStrength.Count < 10)
            return 0.0;

        // Simple autocorrelation to find periodicity
        // BPM range: 60-180 (common music tempo)
        double minBpm = 60.0;
        double maxBpm = 180.0;
        double frameRate = (double)sampleRate / hopSize;

        int minLag = (int)(frameRate * 60.0 / maxBpm); // frames per beat at max BPM
        int maxLag = (int)(frameRate * 60.0 / minBpm); // frames per beat at min BPM

        double maxCorrelation = 0.0;
        int bestLag = minLag;

        for (int lag = minLag; lag <= maxLag && lag < onsetStrength.Count / 2; lag++)
        {
            double correlation = 0.0;
            int count = 0;

            for (int i = 0; i < onsetStrength.Count - lag; i++)
            {
                correlation += onsetStrength[i] * onsetStrength[i + lag];
                count++;
            }

            if (count > 0)
            {
                correlation /= count;
                if (correlation > maxCorrelation)
                {
                    maxCorrelation = correlation;
                    bestLag = lag;
                }
            }
        }

        // Convert lag to BPM
        double bpm = (frameRate * 60.0) / bestLag;

        // Sanity check: return 0 if outside reasonable range
        return (bpm >= minBpm && bpm <= maxBpm) ? bpm : 0.0;
    }

    /// <summary>
    /// Apply Hann window function to reduce spectral leakage
    /// Formula: 0.5 * (1 - cos(2π*n / (N-1)))
    /// </summary>
    private static void ApplyHannWindow(float[] samples)
    {
        int N = samples.Length;
        for (int n = 0; n < N; n++)
        {
            double window = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * n / (N - 1)));
            samples[n] *= (float)window;
        }
    }
}
