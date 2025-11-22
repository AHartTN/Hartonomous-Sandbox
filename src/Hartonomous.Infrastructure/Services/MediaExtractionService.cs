using FFMpegCore;
using FFMpegCore.Enums;
using Hartonomous.Core.Models.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Comprehensive media toolkit for extraction, analysis, visualization, and editing.
/// Supports video/audio clipping, format conversion, audio analysis (FFT, waveforms),
/// thumbnail generation, and advanced media operations.
/// </summary>
public class MediaExtractionService
{
    public MediaExtractionService()
    {
        FFmpegHelper.EnsureInitialized();
    }

    /// <summary>
    /// Options for frame extraction from video files.
    /// </summary>
    public class FrameExtractionOptions
    {
        /// <summary>
        /// How many frames per second to extract (e.g., 1.0 = 1 frame every second, 0.5 = 1 frame every 2 seconds)
        /// </summary>
        public double FramesPerSecond { get; set; } = 1.0;

        /// <summary>
        /// Start time for extraction (null = from beginning)
        /// </summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>
        /// End time for extraction (null = until end)
        /// </summary>
        public TimeSpan? EndTime { get; set; }

        /// <summary>
        /// Maximum number of frames to extract (null = no limit)
        /// </summary>
        public int? MaxFrames { get; set; }

        /// <summary>
        /// Output format for frames (PNG, JPEG, BMP, etc.)
        /// </summary>
        public string OutputFormat { get; set; } = "png";

        /// <summary>
        /// Optional resize dimensions (null = keep original size)
        /// </summary>
        public (int Width, int Height)? Resize { get; set; }

        /// <summary>
        /// JPEG quality (1-100) if OutputFormat is "jpeg"
        /// </summary>
        public int JpegQuality { get; set; } = 85;
    }

    /// <summary>
    /// Options for audio extraction from video/audio files.
    /// </summary>
    public class AudioExtractionOptions
    {
        /// <summary>
        /// Start time for extraction (null = from beginning)
        /// </summary>
        public TimeSpan? StartTime { get; set; }

        /// <summary>
        /// Duration of audio to extract (null = until end)
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Output audio format (mp3, wav, flac, aac, ogg, etc.)
        /// </summary>
        public string OutputFormat { get; set; } = "mp3";

        /// <summary>
        /// Audio bitrate in kbps (e.g., 128, 192, 320)
        /// </summary>
        public int? AudioBitrate { get; set; } = 192;

        /// <summary>
        /// Sample rate in Hz (e.g., 44100, 48000)
        /// </summary>
        public int? SampleRate { get; set; }

        /// <summary>
        /// Number of audio channels (1 = mono, 2 = stereo)
        /// </summary>
        public int? Channels { get; set; }

        /// <summary>
        /// Strip video track (force audio-only output)
        /// </summary>
        public bool AudioOnly { get; set; } = true;
    }

    /// <summary>
    /// Extracts frames from a video file with filtering options.
    /// </summary>
    /// <param name="videoPath">Path to input video file</param>
    /// <param name="outputDirectory">Directory to save extracted frames</param>
    /// <param name="options">Frame extraction options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of extracted frame paths with timestamps</returns>
    public async Task<List<(TimeSpan Timestamp, string FilePath)>> ExtractFramesAsync(
        string videoPath,
        string outputDirectory,
        FrameExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new FrameExtractionOptions();
        Directory.CreateDirectory(outputDirectory);

        // Analyze video to get duration and dimensions
        var mediaInfo = await FFProbe.AnalyseAsync(videoPath, cancellationToken: cancellationToken);
        var duration = mediaInfo.Duration;
        var startTime = options.StartTime ?? TimeSpan.Zero;
        var endTime = options.EndTime ?? duration;
        var frameInterval = TimeSpan.FromSeconds(1.0 / options.FramesPerSecond);

        var extractedFrames = new List<(TimeSpan, string)>();
        var frameCount = 0;

        for (var timestamp = startTime; timestamp < endTime; timestamp += frameInterval)
        {
            if (options.MaxFrames.HasValue && frameCount >= options.MaxFrames.Value)
                break;

            cancellationToken.ThrowIfCancellationRequested();

            var outputFileName = $"frame_{frameCount:D6}_{timestamp.TotalSeconds:F3}s.{options.OutputFormat}";
            var outputPath = Path.Combine(outputDirectory, outputFileName);

            // Build FFmpeg arguments with optional resize
            var arguments = FFMpegArguments
                .FromFileInput(videoPath, verifyExists: true, inputOpts => inputOpts
                    .Seek(timestamp))
                .OutputToFile(outputPath, overwrite: true, outputOpts =>
                {
                    outputOpts.WithFrameOutputCount(1); // Extract single frame

                    if (options.Resize.HasValue)
                    {
                        outputOpts.WithVideoFilters(filters => filters
                            .Scale(options.Resize.Value.Width, options.Resize.Value.Height));
                    }

                    if (options.OutputFormat.ToLower() == "jpeg" || options.OutputFormat.ToLower() == "jpg")
                    {
                        outputOpts.WithCustomArgument($"-q:v {(100 - options.JpegQuality) / 10}"); // FFmpeg quality scale
                    }
                });

            await arguments
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            if (File.Exists(outputPath))
            {
                extractedFrames.Add((timestamp, outputPath));
                frameCount++;
            }
        }

        return extractedFrames;
    }

    /// <summary>
    /// Extracts audio from a video or audio file with filtering options.
    /// </summary>
    /// <param name="inputPath">Path to input video/audio file</param>
    /// <param name="outputPath">Path to output audio file</param>
    /// <param name="options">Audio extraction options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if extraction succeeded</returns>
    public async Task<bool> ExtractAudioAsync(
        string inputPath,
        string outputPath,
        AudioExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new AudioExtractionOptions();

        var arguments = FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true, inputOptions =>
            {
                if (options.StartTime.HasValue)
                {
                    inputOptions.Seek(options.StartTime.Value);
                }
            })
            .OutputToFile(outputPath, overwrite: true, outputOptions =>
            {
                // Duration/trim
                if (options.Duration.HasValue)
                {
                    outputOptions.WithDuration(options.Duration.Value);
                }

                // Audio codec based on format
                string audioCodec = options.OutputFormat.ToLower() switch
                {
                    "mp3" => "libmp3lame",
                    "aac" => "aac",
                    "flac" => "flac",
                    "ogg" or "vorbis" => "libvorbis",
                    "wav" => "pcm_s16le", // PCM codec for WAV
                    _ => "libmp3lame"
                };

                outputOptions.WithAudioCodec(audioCodec);

                // Bitrate (not applicable to lossless formats like FLAC/WAV)
                if (options.AudioBitrate.HasValue && options.OutputFormat.ToLower() != "flac" && options.OutputFormat.ToLower() != "wav")
                {
                    outputOptions.WithAudioBitrate(options.AudioBitrate.Value);
                }

                // Sample rate
                if (options.SampleRate.HasValue)
                {
                    outputOptions.WithAudioSamplingRate(options.SampleRate.Value);
                }

                // Channels (mono/stereo)
                if (options.Channels.HasValue)
                {
                    outputOptions.WithCustomArgument($"-ac {options.Channels.Value}");
                }

                // Strip video track
                if (options.AudioOnly)
                {
                    outputOptions.DisableChannel(Channel.Video);
                }
            });

        return await arguments
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();
    }

    /// <summary>
    /// Extracts a specific time segment from a video file.
    /// </summary>
    /// <param name="inputPath">Path to input video file</param>
    /// <param name="outputPath">Path to output video file</param>
    /// <param name="startTime">Start time of segment</param>
    /// <param name="duration">Duration of segment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if extraction succeeded</returns>
    public async Task<bool> ExtractVideoSegmentAsync(
        string inputPath,
        string outputPath,
        TimeSpan startTime,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var success = await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true, options => options
                .Seek(startTime))
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithDuration(duration)
                .CopyChannel(Channel.Both)) // Copy streams without re-encoding (fast)
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    /// <summary>
    /// Creates a GIF from a video segment.
    /// </summary>
    /// <param name="inputPath">Path to input video file</param>
    /// <param name="outputPath">Path to output GIF file</param>
    /// <param name="startTime">Start time (null = beginning)</param>
    /// <param name="duration">Duration (null = 3 seconds default)</param>
    /// <param name="fps">Frames per second (default 10)</param>
    /// <param name="width">Output width in pixels (null = keep original, maintains aspect ratio)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if creation succeeded</returns>
    public async Task<bool> CreateGifAsync(
        string inputPath,
        string outputPath,
        TimeSpan? startTime = null,
        TimeSpan? duration = null,
        int fps = 10,
        int? width = null,
        CancellationToken cancellationToken = default)
    {
        duration ??= TimeSpan.FromSeconds(3);

        var success = await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true, options =>
            {
                if (startTime.HasValue)
                {
                    options.Seek(startTime.Value);
                }
            })
            .OutputToFile(outputPath, overwrite: true, options =>
            {
                options
                    .WithDuration(duration.Value)
                    .WithFramerate(fps);

                if (width.HasValue)
                {
                    options.WithVideoFilters(filters => filters.Scale(width.Value, -1)); // -1 = maintain aspect ratio
                }
            })
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    #region Audio Analysis & Visualization

    /// <summary>
    /// Extracts audio waveform data for visualization.
    /// Returns amplitude values suitable for drawing waveforms.
    /// </summary>
    /// <param name="audioPath">Path to audio file</param>
    /// <param name="sampleCount">Number of samples to extract (resolution of waveform)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of amplitude values (-1.0 to 1.0)</returns>
    public async Task<double[]> ExtractWaveformAsync(
        string audioPath,
        int sampleCount = 1000,
        CancellationToken cancellationToken = default)
    {
        var tempPcmPath = Path.Combine(Path.GetTempPath(), $"waveform_{Guid.NewGuid()}.pcm");

        try
        {
            // Convert to raw PCM
            var success = await FFMpegArguments
                .FromFileInput(audioPath, verifyExists: true)
                .OutputToFile(tempPcmPath, overwrite: true, options => options
                    .WithAudioCodec("pcm_s16le")
                    .WithAudioSamplingRate(44100)
                    .WithCustomArgument("-ac 1") // Mono
                    .ForceFormat("s16le"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            if (!success)
                throw new InvalidOperationException("Failed to extract PCM data");

            // Read PCM samples
            var pcmData = await File.ReadAllBytesAsync(tempPcmPath, cancellationToken);
            var totalSamples = pcmData.Length / 2; // 16-bit = 2 bytes per sample

            // Downsample to requested sample count
            var samplesPerBucket = totalSamples / sampleCount;
            var waveform = new double[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                var startSample = i * samplesPerBucket;
                var endSample = Math.Min(startSample + samplesPerBucket, totalSamples);

                // Calculate RMS (root mean square) for this bucket
                double sumSquares = 0;
                int count = 0;

                for (int s = startSample; s < endSample; s++)
                {
                    var sampleIndex = s * 2;
                    if (sampleIndex + 1 < pcmData.Length)
                    {
                        var sample = BitConverter.ToInt16(pcmData, sampleIndex);
                        var normalized = sample / 32768.0; // Normalize to -1.0 to 1.0
                        sumSquares += normalized * normalized;
                        count++;
                    }
                }

                waveform[i] = count > 0 ? Math.Sqrt(sumSquares / count) : 0;
            }

            return waveform;
        }
        finally
        {
            if (File.Exists(tempPcmPath))
                File.Delete(tempPcmPath);
        }
    }

    /// <summary>
    /// Performs Fast Fourier Transform (FFT) on audio to extract frequency spectrum.
    /// Useful for spectrograms, frequency analysis, and audio visualization.
    /// </summary>
    /// <param name="audioPath">Path to audio file</param>
    /// <param name="startTime">Start time for analysis</param>
    /// <param name="duration">Duration to analyze (null = entire file)</param>
    /// <param name="fftSize">FFT window size (must be power of 2, e.g., 2048, 4096)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Frequency spectrum data</returns>
    public async Task<FrequencySpectrum> AnalyzeFrequencySpectrumAsync(
        string audioPath,
        TimeSpan? startTime = null,
        TimeSpan? duration = null,
        int fftSize = 2048,
        CancellationToken cancellationToken = default)
    {
        var tempPcmPath = Path.Combine(Path.GetTempPath(), $"fft_{Guid.NewGuid()}.pcm");

        try
        {
            // Extract PCM segment
            var arguments = FFMpegArguments
                .FromFileInput(audioPath, verifyExists: true, options =>
                {
                    if (startTime.HasValue)
                        options.Seek(startTime.Value);
                })
                .OutputToFile(tempPcmPath, overwrite: true, options =>
                {
                    if (duration.HasValue)
                        options.WithDuration(duration.Value);

                    options
                        .WithAudioCodec("pcm_s16le")
                        .WithAudioSamplingRate(44100)
                        .WithCustomArgument("-ac 1") // Mono
                        .ForceFormat("s16le");
                });

            await arguments
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            // Read PCM data
            var pcmData = await File.ReadAllBytesAsync(tempPcmPath, cancellationToken);
            var sampleCount = pcmData.Length / 2;

            // Convert to samples
            var samples = new double[Math.Min(sampleCount, fftSize)];
            for (int i = 0; i < samples.Length; i++)
            {
                var sampleIndex = i * 2;
                if (sampleIndex + 1 < pcmData.Length)
                {
                    var sample = BitConverter.ToInt16(pcmData, sampleIndex);
                    samples[i] = sample / 32768.0;
                }
            }

            // Apply Hamming window to reduce spectral leakage
            ApplyHammingWindow(samples);

            // Perform FFT
            var fftResult = PerformFFT(samples);

            // Calculate magnitude spectrum (only first half is meaningful)
            var magnitudes = new double[fftResult.Length / 2];
            for (int i = 0; i < magnitudes.Length; i++)
            {
                var real = fftResult[i].Real;
                var imag = fftResult[i].Imaginary;
                magnitudes[i] = Math.Sqrt(real * real + imag * imag);
            }

            // Convert to decibels
            var decibels = magnitudes.Select(m => 20 * Math.Log10(Math.Max(m, 1e-10))).ToArray();

            // Map frequency bins
            const int sampleRate = 44100;
            var frequencyResolution = sampleRate / (double)fftSize;
            var frequencies = Enumerable.Range(0, magnitudes.Length)
                .Select(i => i * frequencyResolution)
                .ToArray();

            return new FrequencySpectrum
            {
                Frequencies = frequencies,
                Magnitudes = magnitudes,
                Decibels = decibels,
                SampleRate = sampleRate,
                FftSize = fftSize,
                FrequencyResolution = frequencyResolution
            };
        }
        finally
        {
            if (File.Exists(tempPcmPath))
                File.Delete(tempPcmPath);
        }
    }

    /// <summary>
    /// Generates a spectrogram (time-frequency heatmap) of an audio file.
    /// Returns 2D array where [time][frequency] = magnitude.
    /// </summary>
    public async Task<Spectrogram> GenerateSpectrogramAsync(
        string audioPath,
        int fftSize = 2048,
        int hopSize = 512,
        CancellationToken cancellationToken = default)
    {
        var tempPcmPath = Path.Combine(Path.GetTempPath(), $"spectrogram_{Guid.NewGuid()}.pcm");

        try
        {
            // Extract PCM
            await FFMpegArguments
                .FromFileInput(audioPath, verifyExists: true)
                .OutputToFile(tempPcmPath, overwrite: true, options => options
                    .WithAudioCodec("pcm_s16le")
                    .WithAudioSamplingRate(44100)
                    .WithCustomArgument("-ac 1")
                    .ForceFormat("s16le"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            var pcmData = await File.ReadAllBytesAsync(tempPcmPath, cancellationToken);
            var totalSamples = pcmData.Length / 2;

            // Calculate number of time frames
            var frameCount = (totalSamples - fftSize) / hopSize + 1;
            var frequencyBins = fftSize / 2;

            var spectrogram = new double[frameCount][];

            // Process each time frame
            for (int frame = 0; frame < frameCount; frame++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var startSample = frame * hopSize;
                var samples = new double[fftSize];

                // Extract window
                for (int i = 0; i < fftSize && startSample + i < totalSamples; i++)
                {
                    var sampleIndex = (startSample + i) * 2;
                    if (sampleIndex + 1 < pcmData.Length)
                    {
                        var sample = BitConverter.ToInt16(pcmData, sampleIndex);
                        samples[i] = sample / 32768.0;
                    }
                }

                // Apply window and FFT
                ApplyHammingWindow(samples);
                var fftResult = PerformFFT(samples);

                // Calculate magnitude spectrum
                spectrogram[frame] = new double[frequencyBins];
                for (int i = 0; i < frequencyBins; i++)
                {
                    var real = fftResult[i].Real;
                    var imag = fftResult[i].Imaginary;
                    var magnitude = Math.Sqrt(real * real + imag * imag);
                    spectrogram[frame][i] = 20 * Math.Log10(Math.Max(magnitude, 1e-10)); // Convert to dB
                }
            }

            const int sampleRate = 44100;
            var frequencyResolution = sampleRate / (double)fftSize;
            var timeResolution = hopSize / (double)sampleRate;

            return new Spectrogram
            {
                Data = spectrogram,
                TimeFrames = frameCount,
                FrequencyBins = frequencyBins,
                SampleRate = sampleRate,
                FftSize = fftSize,
                HopSize = hopSize,
                FrequencyResolution = frequencyResolution,
                TimeResolution = timeResolution
            };
        }
        finally
        {
            if (File.Exists(tempPcmPath))
                File.Delete(tempPcmPath);
        }
    }

    /// <summary>
    /// Analyzes audio characteristics like RMS level, peak amplitude, zero crossing rate, etc.
    /// </summary>
    public async Task<AudioAnalysis> AnalyzeAudioAsync(
        string audioPath,
        CancellationToken cancellationToken = default)
    {
        var tempPcmPath = Path.Combine(Path.GetTempPath(), $"analysis_{Guid.NewGuid()}.pcm");

        try
        {
            // Get audio info
            var mediaInfo = await FFProbe.AnalyseAsync(audioPath, cancellationToken: cancellationToken);
            var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

            // Extract PCM
            await FFMpegArguments
                .FromFileInput(audioPath, verifyExists: true)
                .OutputToFile(tempPcmPath, overwrite: true, options => options
                    .WithAudioCodec("pcm_s16le")
                    .WithAudioSamplingRate(44100)
                    .WithCustomArgument("-ac 1")
                    .ForceFormat("s16le"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            var pcmData = await File.ReadAllBytesAsync(tempPcmPath, cancellationToken);
            var sampleCount = pcmData.Length / 2;

            // Calculate metrics
            double sumSquares = 0;
            double peak = 0;
            int zeroCrossings = 0;
            int previousSign = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                var sampleIndex = i * 2;
                if (sampleIndex + 1 < pcmData.Length)
                {
                    var sample = BitConverter.ToInt16(pcmData, sampleIndex);
                    var normalized = sample / 32768.0;

                    // RMS calculation
                    sumSquares += normalized * normalized;

                    // Peak detection
                    var abs = Math.Abs(normalized);
                    if (abs > peak)
                        peak = abs;

                    // Zero crossing rate
                    var sign = Math.Sign(sample);
                    if (previousSign != 0 && sign != previousSign)
                        zeroCrossings++;
                    previousSign = sign;
                }
            }

            var rms = Math.Sqrt(sumSquares / sampleCount);
            var rmsDb = 20 * Math.Log10(Math.Max(rms, 1e-10));
            var peakDb = 20 * Math.Log10(Math.Max(peak, 1e-10));
            var zeroCrossingRate = zeroCrossings / (double)sampleCount;

            return new AudioAnalysis
            {
                Duration = mediaInfo.Duration,
                SampleRate = audioStream?.SampleRateHz ?? 44100,
                Channels = audioStream?.Channels ?? 1,
                BitDepth = audioStream?.BitDepth ?? 16,
                Codec = audioStream?.CodecName ?? "unknown",
                Bitrate = audioStream?.BitRate ?? 0,
                RmsLevel = rms,
                RmsLevelDb = rmsDb,
                PeakLevel = peak,
                PeakLevelDb = peakDb,
                ZeroCrossingRate = zeroCrossingRate,
                DynamicRange = peakDb - rmsDb,
                TotalSamples = sampleCount
            };
        }
        finally
        {
            if (File.Exists(tempPcmPath))
                File.Delete(tempPcmPath);
        }
    }

    private void ApplyHammingWindow(double[] samples)
    {
        var n = samples.Length;
        for (int i = 0; i < n; i++)
        {
            samples[i] *= 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (n - 1));
        }
    }

    private Complex[] PerformFFT(double[] samples)
    {
        var n = samples.Length;
        
        // Ensure power of 2
        var fftSize = 1;
        while (fftSize < n)
            fftSize *= 2;

        var complex = new Complex[fftSize];
        for (int i = 0; i < n; i++)
            complex[i] = new Complex(samples[i], 0);

        // Cooley-Tukey FFT algorithm
        FFTRecursive(complex);

        return complex;
    }

    private void FFTRecursive(Complex[] buffer)
    {
        var n = buffer.Length;
        if (n <= 1) return;

        // Divide
        var even = new Complex[n / 2];
        var odd = new Complex[n / 2];
        for (int i = 0; i < n / 2; i++)
        {
            even[i] = buffer[i * 2];
            odd[i] = buffer[i * 2 + 1];
        }

        // Conquer
        FFTRecursive(even);
        FFTRecursive(odd);

        // Combine
        for (int k = 0; k < n / 2; k++)
        {
            var t = Complex.FromPolarCoordinates(1.0, -2 * Math.PI * k / n) * odd[k];
            buffer[k] = even[k] + t;
            buffer[k + n / 2] = even[k] - t;
        }
    }

    #endregion

    #region Video Clipping & Editing

    /// <summary>
    /// Concatenates multiple video files into a single output.
    /// </summary>
    public async Task<bool> ConcatenateVideosAsync(
        string[] inputPaths,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var tempListPath = Path.Combine(Path.GetTempPath(), $"concat_{Guid.NewGuid()}.txt");

        try
        {
            // Create concat file
            var lines = inputPaths.Select(p => $"file '{p.Replace("'", @"'\''")}'");
            await File.WriteAllLinesAsync(tempListPath, lines, cancellationToken);

            // Concatenate
            var success = await FFMpegArguments
                .FromFileInput(tempListPath, verifyExists: true, options => options
                    .WithCustomArgument("-f concat")
                    .WithCustomArgument("-safe 0"))
                .OutputToFile(outputPath, overwrite: true, options => options
                    .CopyChannel(Channel.Both))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            return success;
        }
        finally
        {
            if (File.Exists(tempListPath))
                File.Delete(tempListPath);
        }
    }

    /// <summary>
    /// Creates a video collage/grid from multiple input videos.
    /// </summary>
    public async Task<bool> CreateVideoGridAsync(
        string[] inputPaths,
        string outputPath,
        int columns = 2,
        CancellationToken cancellationToken = default)
    {
        if (inputPaths.Length < 2)
            throw new ArgumentException("Need at least 2 videos for grid");

        var rows = (int)Math.Ceiling(inputPaths.Length / (double)columns);

        // Build complex filter for grid layout
        var inputs = string.Join("", inputPaths.Select((_, i) => $"[{i}:v]"));
        var xstackInputs = string.Join("", Enumerable.Range(0, inputPaths.Length).Select(i => $"[v{i}]"));
        
        // Scale all videos to same size
        var filterComplex = string.Join(";", inputPaths.Select((_, i) => 
            $"[{i}:v]scale=640:360[v{i}]"));
        
        // Create grid layout
        var layout = string.Join("|", Enumerable.Range(0, inputPaths.Length).Select(i =>
        {
            var row = i / columns;
            var col = i % columns;
            return $"{col * 640}_{row * 360}";
        }));

        filterComplex += $";{xstackInputs}xstack=inputs={inputPaths.Length}:layout={layout}[v]";

        var arguments = FFMpegArguments.FromFileInput(inputPaths[0], verifyExists: true);
        
        for (int i = 1; i < inputPaths.Length; i++)
        {
            arguments = arguments.AddFileInput(inputPaths[i]);
        }

        var success = await arguments
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument($"-filter_complex \"{filterComplex}\"")
                .WithCustomArgument("-map \"[v]\"")
                .WithVideoCodec("libx264"))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    /// <summary>
    /// Adds text overlay to video.
    /// </summary>
    public async Task<bool> AddTextOverlayAsync(
        string inputPath,
        string outputPath,
        string text,
        string position = "center",
        string fontColor = "white",
        int fontSize = 24,
        CancellationToken cancellationToken = default)
    {
        // Map position to coordinates
        var positionMap = new Dictionary<string, string>
        {
            ["topleft"] = "x=10:y=10",
            ["topright"] = "x=w-tw-10:y=10",
            ["center"] = "x=(w-tw)/2:y=(h-th)/2",
            ["bottomleft"] = "x=10:y=h-th-10",
            ["bottomright"] = "x=w-tw-10:y=h-th-10"
        };

        var posStr = positionMap.GetValueOrDefault(position.ToLower(), positionMap["center"]);
        var drawtext = $"drawtext=text='{text.Replace("'", @"\'")}':fontsize={fontSize}:fontcolor={fontColor}:{posStr}";

        var success = await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument($"-vf \"{drawtext}\""))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    /// <summary>
    /// Changes video playback speed.
    /// </summary>
    public async Task<bool> ChangeVideoSpeedAsync(
        string inputPath,
        string outputPath,
        double speedFactor = 1.0,
        CancellationToken cancellationToken = default)
    {
        if (speedFactor <= 0)
            throw new ArgumentException("Speed factor must be positive");

        // Video speed filter
        var videoFilter = $"setpts={1.0 / speedFactor}*PTS";
        
        // Audio speed filter
        var audioFilter = $"atempo={speedFactor}";

        // Handle extreme speeds (atempo only supports 0.5-2.0 range)
        if (speedFactor < 0.5 || speedFactor > 2.0)
        {
            var stages = (int)Math.Ceiling(Math.Log(speedFactor, 2));
            var stageSpeed = Math.Pow(speedFactor, 1.0 / stages);
            audioFilter = string.Join(",", Enumerable.Repeat($"atempo={stageSpeed}", stages));
        }

        var success = await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument($"-filter:v \"{videoFilter}\"")
                .WithCustomArgument($"-filter:a \"{audioFilter}\""))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    /// <summary>
    /// Generates thumbnail contact sheet from video (grid of frames).
    /// </summary>
    public async Task<bool> GenerateContactSheetAsync(
        string inputPath,
        string outputPath,
        int columns = 4,
        int rows = 4,
        CancellationToken cancellationToken = default)
    {
        var totalFrames = columns * rows;
        var tileFilter = $"select='not(mod(n\\,{totalFrames}))',scale=320:240,tile={columns}x{rows}";

        var success = await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument($"-vf \"{tileFilter}\"")
                .WithFrameOutputCount(1))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    #endregion

    #region Audio Effects

    /// <summary>
    /// Normalizes audio volume to target level.
    /// </summary>
    public async Task<bool> NormalizeAudioAsync(
        string inputPath,
        string outputPath,
        double targetLoudnessDb = -16.0,
        CancellationToken cancellationToken = default)
    {
        var success = await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument($"-af loudnorm=I={targetLoudnessDb}"))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    /// <summary>
    /// Applies fade in/out effect to audio.
    /// </summary>
    public async Task<bool> FadeAudioAsync(
        string inputPath,
        string outputPath,
        TimeSpan? fadeInDuration = null,
        TimeSpan? fadeOutDuration = null,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<string>();

        if (fadeInDuration.HasValue)
        {
            filters.Add($"afade=t=in:st=0:d={fadeInDuration.Value.TotalSeconds}");
        }

        if (fadeOutDuration.HasValue)
        {
            var mediaInfo = await FFProbe.AnalyseAsync(inputPath, cancellationToken: cancellationToken);
            var startTime = mediaInfo.Duration.TotalSeconds - fadeOutDuration.Value.TotalSeconds;
            filters.Add($"afade=t=out:st={startTime}:d={fadeOutDuration.Value.TotalSeconds}");
        }

        if (filters.Count == 0)
            return false;

        var filterString = string.Join(",", filters);

        var success = await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument($"-af \"{filterString}\""))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    /// <summary>
    /// Removes silence from audio.
    /// </summary>
    public async Task<bool> RemoveSilenceAsync(
        string inputPath,
        string outputPath,
        double silenceThresholdDb = -40.0,
        double minSilenceDuration = 0.5,
        CancellationToken cancellationToken = default)
    {
        var filter = $"silenceremove=stop_periods=-1:stop_duration={minSilenceDuration}:stop_threshold={silenceThresholdDb}dB";

        var success = await FFMpegArguments
            .FromFileInput(inputPath, verifyExists: true)
            .OutputToFile(outputPath, overwrite: true, options => options
                .WithCustomArgument($"-af \"{filter}\""))
            .CancellableThrough(cancellationToken)
            .ProcessAsynchronously();

        return success;
    }

    #endregion
}
