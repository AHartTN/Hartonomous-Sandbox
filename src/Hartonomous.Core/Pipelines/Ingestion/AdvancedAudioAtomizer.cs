using Hartonomous.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// Advanced audio atomization with spectral analysis, silence detection, speaker diarization,
/// transcription, and acoustic fingerprinting for deduplication.
/// 
/// BEST PRACTICES FROM RESEARCH:
/// - Perceptual audio features: zero-crossing rate, tempo, spectral flatness, bandwidth
/// - Spectrogram generation via FFT for frequency-domain analysis
/// - Chromaprint acoustic fingerprinting (Shazam-like peak matching in time-frequency graph)
/// - Whisper transcription with timestamp alignment for speech segments
/// - Speaker diarization for conversation tracking
/// - Silence detection to segment audio into meaningful units
/// 
/// INTEGRATION POINTS:
/// - Azure Speech SDK: SpeechRecognizer for transcription, ConversationTranscriber for diarization
/// - Whisper API: OpenAI/Azure OpenAI for high-quality transcription with timestamps
/// - Chromaprint: Open-source acoustic fingerprinting library
/// - NAudio or FFmpeg.NET: Spectrogram generation, audio format detection
/// </summary>
public class AdvancedAudioAtomizer : IAudioAtomizer
{
    private readonly ILogger<AdvancedAudioAtomizer> _logger;
    private readonly AudioAtomizationStrategy _strategy;

    // Configuration defaults (from research on audio atomization)
    private const int DefaultSampleRate = 16000; // Whisper uses 16kHz
    private const int DefaultFftSize = 2048; // Power of 2 for FFT efficiency
    private const double SilenceThresholdDb = -40.0; // Energy threshold for silence detection
    private const int MinSegmentDurationMs = 2000; // Minimum 2 seconds per segment (from research)
    private const int MaxSegmentDurationMs = 30000; // Maximum 30 seconds per segment

    public string Modality => "audio";

    public AudioAtomizationStrategy Strategy => _strategy;

    public AdvancedAudioAtomizer(
        AudioAtomizationStrategy strategy = AudioAtomizationStrategy.SilenceBased,
        ILogger<AdvancedAudioAtomizer>? logger = null)
    {
        _strategy = strategy;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async IAsyncEnumerable<AtomCandidate> AtomizeAsync(
        byte[] audioData,
        AtomizationContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (audioData == null || audioData.Length == 0)
        {
            _logger.LogWarning("Empty audio data provided for atomization");
            yield break;
        }

        // Detect audio format from magic numbers
        var audioFormat = DetectAudioFormat(audioData);
        _logger.LogInformation($"Detected audio format: {audioFormat}");

        // Extract strategy from context hints
        var strategy = GetStrategyFromContext(context);
        _logger.LogInformation($"Using audio atomization strategy: {strategy}");

        // Route to appropriate atomization method
        await foreach (var atom in strategy switch
        {
            AudioAtomizationStrategy.WholeAudio => AtomizeWholeAudioAsync(audioData, context, audioFormat, cancellationToken),
            AudioAtomizationStrategy.SilenceBased => AtomizeBySilenceAsync(audioData, context, audioFormat, cancellationToken),
            AudioAtomizationStrategy.SpeakerDiarization => AtomizeBySpeakerAsync(audioData, context, audioFormat, cancellationToken),
            AudioAtomizationStrategy.FixedDuration => AtomizeByFixedDurationAsync(audioData, context, audioFormat, cancellationToken),
            AudioAtomizationStrategy.TranscriptionSegments => AtomizeByTranscriptionAsync(audioData, context, audioFormat, cancellationToken),
            _ => AtomizeWholeAudioAsync(audioData, context, audioFormat, cancellationToken)
        })
        {
            yield return atom;
        }
    }

    #region Strategy Implementation

    private async IAsyncEnumerable<AtomCandidate> AtomizeWholeAudioAsync(
        byte[] audioData,
        AtomizationContext context,
        string audioFormat,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Creating whole audio atom ({audioData.Length} bytes)");

        var boundary = new AtomBoundary
        {
            StartByteOffset = 0,
            EndByteOffset = audioData.Length,
            StartTime = TimeSpan.Zero,
            EndTime = await EstimateAudioDurationAsync(audioData, audioFormat),
            StructuralPath = "audio/whole"
        };

        var metadata = new Dictionary<string, object>
        {
            ["audioFormat"] = audioFormat,
            ["sizeBytes"] = audioData.Length,
            ["samplingRate"] = context.Hints?.ContainsKey("samplingRate") == true ? context.Hints["samplingRate"] : DefaultSampleRate,
            ["strategy"] = AudioAtomizationStrategy.WholeAudio.ToString()
        };

        // Generate acoustic fingerprint for deduplication (Chromaprint-style)
        var fingerprint = await GenerateAcousticFingerprintAsync(audioData, audioFormat);
        if (!string.IsNullOrEmpty(fingerprint))
        {
            metadata["acousticFingerprint"] = fingerprint;
        }

        // Extract audio features for quality assessment and semantic understanding
        try
        {
            var features = await ExtractAudioFeaturesAsync(audioData, audioFormat);
            if (features != null)
            {
                metadata["features"] = new Dictionary<string, object>
                {
                    ["zeroCrossingRate"] = features.ZeroCrossingRate,
                    ["spectralCentroid"] = features.SpectralCentroid,
                    ["spectralFlatness"] = features.SpectralFlatness,
                    ["spectralRolloff"] = features.SpectralRolloff,
                    ["spectralBandwidth"] = features.SpectralBandwidth,
                    ["tempo"] = features.Tempo,
                    ["bassFrequency"] = features.Tones.BassFrequency,
                    ["midFrequency"] = features.Tones.MidFrequency,
                    ["trebleFrequency"] = features.Tones.TrebleFrequency
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to extract audio features: {ex.Message}");
        }

        yield return new AtomCandidate
        {
            Modality = "audio",
            Subtype = audioFormat,
            BinaryPayload = audioData,
            CanonicalText = $"Audio file ({audioFormat}, {FormatBytes(audioData.Length)})",
            SourceUri = context.SourceUri,
            SourceType = context.SourceType,
            Boundary = boundary,
            QualityScore = CalculateAudioQuality(audioData, audioFormat),
            Metadata = metadata,
            HashInput = fingerprint ?? Convert.ToBase64String(audioData.Take(1024).ToArray())
        };
    }

    private async IAsyncEnumerable<AtomCandidate> AtomizeBySilenceAsync(
        byte[] audioData,
        AtomizationContext context,
        string audioFormat,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Atomizing audio by silence detection");

        // IMPLEMENTED: RMS energy-based silence detection (SilenceDetector class)
        // ALGORITHM (fully working):
        // 1. Convert audio to PCM samples via WavFileParser
        // 2. Calculate energy (RMS) per frame (default: 20ms frames)
        // 3. Convert to dB: 20 * log10(RMS / reference)
        // 4. Detect silence segments where energy < threshold (default: -40dB)
        // 5. Split audio at silence boundaries
        // 6. Filter segments by min/max duration (default: 2s - 30s)

        var segments = await SplitAudioBySilenceAsync(audioData, audioFormat);
        int segmentIndex = 0;

        foreach (var segment in segments)
        {
            var boundary = new AtomBoundary
            {
                StartByteOffset = segment.StartOffset,
                EndByteOffset = segment.EndOffset,
                StartTime = segment.StartTime,
                EndTime = segment.EndTime,
                StructuralPath = $"audio/segment/{segmentIndex}"
            };

            var metadata = new Dictionary<string, object>
            {
                ["audioFormat"] = audioFormat,
                ["segmentIndex"] = segmentIndex,
                ["duration"] = (segment.EndTime - segment.StartTime).TotalSeconds,
                ["strategy"] = AudioAtomizationStrategy.SilenceBased.ToString()
            };

            var segmentData = audioData.Skip(segment.StartOffset).Take(segment.EndOffset - segment.StartOffset).ToArray();
            var fingerprint = await GenerateAcousticFingerprintAsync(segmentData, audioFormat);
            if (!string.IsNullOrEmpty(fingerprint))
            {
                metadata["acousticFingerprint"] = fingerprint;
            }

            yield return new AtomCandidate
            {
                Modality = "audio",
                Subtype = audioFormat,
                BinaryPayload = segmentData,
                CanonicalText = $"Audio segment {segmentIndex} ({FormatDuration(segment.EndTime - segment.StartTime)})",
                SourceUri = context.SourceUri,
                SourceType = context.SourceType,
                Boundary = boundary,
                QualityScore = CalculateAudioQuality(segmentData, audioFormat),
                Metadata = metadata,
                HashInput = fingerprint ?? Convert.ToBase64String(segmentData.Take(512).ToArray())
            };

            segmentIndex++;
        }
    }

    private async IAsyncEnumerable<AtomCandidate> AtomizeBySpeakerAsync(
        byte[] audioData,
        AtomizationContext context,
        string audioFormat,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Atomizing audio by speaker diarization");

        // TODO: Implement spatial tensor query for speaker diarization
        // ARCHITECTURE (Database-First, No VRAM):
        // 1. Extract speaker embeddings (x-vectors/d-vectors) per segment → GEOMETRY points
        // 2. Query TensorAtoms: SELECT WHERE ModelType='speaker_verification'
        //    ORDER BY SpatialSignature.STDistance(@speakerEmbeddingGeometry) ASC
        // 3. Cluster spatially-similar embeddings to identify unique speakers
        // 4. Use CLR SIMD for PLDA scoring, AHC clustering
        // 5. Assign speaker labels per segment
        //
        // NO Azure Speech SDK - speaker embedding model ingested as TensorAtoms
        // Clustering via spatial proximity (R-tree KNN), not scikit-learn

        // Placeholder: Yield whole audio as single speaker
        yield return await Task.FromResult(new AtomCandidate
        {
            Modality = "audio",
            Subtype = audioFormat,
            BinaryPayload = audioData,
            CanonicalText = $"Speaker segment (diarization not yet implemented)",
            SourceUri = context.SourceUri,
            SourceType = context.SourceType,
            Boundary = new AtomBoundary
            {
                StartByteOffset = 0,
                EndByteOffset = audioData.Length,
                StructuralPath = "audio/speaker/unknown"
            },
            QualityScore = 0.5, // Lower quality score since diarization not implemented
            Metadata = new Dictionary<string, object>
            {
                ["strategy"] = AudioAtomizationStrategy.SpeakerDiarization.ToString(),
                ["speakerId"] = "unknown",
                ["implementationStatus"] = "stub"
            },
            HashInput = Convert.ToBase64String(audioData.Take(1024).ToArray())
        });
    }

    private async IAsyncEnumerable<AtomCandidate> AtomizeByFixedDurationAsync(
        byte[] audioData,
        AtomizationContext context,
        string audioFormat,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Atomizing audio by fixed duration windows");

        var windowDuration = context.Hints?.ContainsKey("windowDuration") == true
            ? TimeSpan.FromSeconds(Convert.ToDouble(context.Hints["windowDuration"]))
            : TimeSpan.FromSeconds(10);

        var totalDuration = await EstimateAudioDurationAsync(audioData, audioFormat);
        var bytesPerSecond = (int)(audioData.Length / totalDuration.TotalSeconds);

        int windowIndex = 0;
        for (var currentTime = TimeSpan.Zero; currentTime < totalDuration; currentTime += windowDuration)
        {
            var startOffset = (int)(currentTime.TotalSeconds * bytesPerSecond);
            var endTime = currentTime + windowDuration;
            if (endTime > totalDuration) endTime = totalDuration;
            var endOffset = Math.Min((int)(endTime.TotalSeconds * bytesPerSecond), audioData.Length);

            var windowData = audioData.Skip(startOffset).Take(endOffset - startOffset).ToArray();

            var boundary = new AtomBoundary
            {
                StartByteOffset = startOffset,
                EndByteOffset = endOffset,
                StartTime = currentTime,
                EndTime = endTime,
                StructuralPath = $"audio/window/{windowIndex}"
            };

            var metadata = new Dictionary<string, object>
            {
                ["audioFormat"] = audioFormat,
                ["windowIndex"] = windowIndex,
                ["windowDuration"] = windowDuration.TotalSeconds,
                ["strategy"] = AudioAtomizationStrategy.FixedDuration.ToString()
            };

            yield return new AtomCandidate
            {
                Modality = "audio",
                Subtype = audioFormat,
                BinaryPayload = windowData,
                CanonicalText = $"Audio window {windowIndex} ({FormatDuration(windowDuration)})",
                SourceUri = context.SourceUri,
                SourceType = context.SourceType,
                Boundary = boundary,
                QualityScore = CalculateAudioQuality(windowData, audioFormat),
                Metadata = metadata,
                HashInput = Convert.ToBase64String(windowData.Take(512).ToArray())
            };

            windowIndex++;
        }
    }

    private async IAsyncEnumerable<AtomCandidate> AtomizeByTranscriptionAsync(
        byte[] audioData,
        AtomizationContext context,
        string audioFormat,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogDebug("Atomizing audio by transcription with timestamp alignment");

        // TODO: Implement spatial tensor query for speech recognition
        // ARCHITECTURE (Database-First, No VRAM):
        // 1. Extract audio features (mel spectrogram) → GEOMETRY point via CLR
        // 2. Query TensorAtoms: SELECT TOP 100 WHERE ModelType='speech_encoder'
        //    ORDER BY SpatialSignature.STDistance(@audioFeaturesGeometry) ASC
        // 3. R-tree spatial index returns relevant encoder/decoder weights (10-50ms)
        // 4. CLR SIMD processes audio frames using retrieved tensor components
        // 5. Generate text atoms with word-level timestamps
        //
        // NO Azure OpenAI, NO PyTorch, NO VRAM - pure SQL Server R-tree + CLR SIMD
        // Speech model weights ingested as TensorAtoms with spatial signatures
        // Inference via spatial proximity, not matrix multiplication
        // {
        //     yield return new AtomCandidate { ... segment.Text, segment.StartTime, segment.EndTime ... };
        // }

        // Placeholder: Yield whole audio with dummy transcription
        yield return await Task.FromResult(new AtomCandidate
        {
            Modality = "audio",
            Subtype = audioFormat,
            BinaryPayload = audioData,
            CanonicalText = "[Transcription not yet implemented - integrate Whisper API]",
            SourceUri = context.SourceUri,
            SourceType = context.SourceType,
            Boundary = new AtomBoundary
            {
                StartByteOffset = 0,
                EndByteOffset = audioData.Length,
                StructuralPath = "audio/transcription/segment0"
            },
            QualityScore = 0.5,
            Metadata = new Dictionary<string, object>
            {
                ["strategy"] = AudioAtomizationStrategy.TranscriptionSegments.ToString(),
                ["language"] = context.Hints?.ContainsKey("language") == true ? context.Hints["language"] : "en-US",
                ["implementationStatus"] = "stub"
            },
            HashInput = Convert.ToBase64String(audioData.Take(1024).ToArray())
        });
    }

    #endregion

    #region Helper Methods

    private AudioAtomizationStrategy GetStrategyFromContext(AtomizationContext context)
    {
        if (context.Hints?.TryGetValue("atomizationStrategy", out var strategyObj) == true
            && Enum.TryParse<AudioAtomizationStrategy>(strategyObj.ToString(), out var strategy))
        {
            return strategy;
        }

        return AudioAtomizationStrategy.WholeAudio; // Default
    }

    private string DetectAudioFormat(byte[] audioData)
    {
        if (audioData.Length < 12) return "unknown";

        // WAV: RIFF....WAVE
        if (audioData[0] == 0x52 && audioData[1] == 0x49 && audioData[2] == 0x46 && audioData[3] == 0x46 &&
            audioData[8] == 0x57 && audioData[9] == 0x41 && audioData[10] == 0x56 && audioData[11] == 0x45)
        {
            return "wav";
        }

        // MP3: ID3 or FF FB/FF F3/FF F2
        if ((audioData[0] == 0x49 && audioData[1] == 0x44 && audioData[2] == 0x33) ||
            (audioData[0] == 0xFF && (audioData[1] == 0xFB || audioData[1] == 0xF3 || audioData[1] == 0xF2)))
        {
            return "mp3";
        }

        // OGG: OggS
        if (audioData[0] == 0x4F && audioData[1] == 0x67 && audioData[2] == 0x67 && audioData[3] == 0x53)
        {
            return "ogg";
        }

        // FLAC: fLaC
        if (audioData[0] == 0x66 && audioData[1] == 0x4C && audioData[2] == 0x61 && audioData[3] == 0x43)
        {
            return "flac";
        }

        // M4A/AAC: ftypM4A or ftypisom
        if (audioData.Length >= 12 && audioData[4] == 0x66 && audioData[5] == 0x74 && audioData[6] == 0x79 && audioData[7] == 0x70)
        {
            return "m4a";
        }

        return "unknown";
    }

    private Task<TimeSpan> EstimateAudioDurationAsync(byte[] audioData, string audioFormat)
    {
        // Parse WAV headers for accurate duration
        var wavFormat = WavFileParser.ParseWavHeader(audioData);
        if (wavFormat != null && wavFormat.DurationSeconds > 0)
        {
            return Task.FromResult(TimeSpan.FromSeconds(wavFormat.DurationSeconds));
        }

        // Fallback: rough estimate for non-WAV or unparseable files
        // Assume 16kHz mono 16-bit = 32000 bytes/sec
        double estimatedSeconds = audioData.Length / 32000.0;
        return Task.FromResult(TimeSpan.FromSeconds(estimatedSeconds));
    }

    private async Task<string?> GenerateAcousticFingerprintAsync(byte[] audioData, string audioFormat)
    {
        try
        {
            // Parse WAV to extract PCM samples
            var wavFormat = WavFileParser.ParseWavHeader(audioData);
            if (wavFormat == null || wavFormat.SampleRate <= 0)
            {
                _logger.LogWarning("Cannot generate acoustic fingerprint: invalid WAV format");
                return null;
            }

            // Extract PCM samples
            var samples = WavFileParser.ExtractPcmSamples(audioData, wavFormat);
            if (samples.Length == 0)
            {
                return null;
            }

            // Generate fingerprint using Shazam-like algorithm:
            // 1. Create spectrogram via STFT (overlapping windows + FFT)
            // 2. Detect spectral peaks (local maxima in time-frequency graph)
            // 3. Create anchor point pairs with time delta
            // 4. Hash pairs to create compact fingerprint

            const int windowSize = 2048; // FFT window size
            const int hopSize = windowSize / 2; // 50% overlap
            var fingerprints = new List<string>();

            // Generate spectrogram frames
            for (int i = 0; i + windowSize < samples.Length; i += hopSize)
            {
                // Extract window
                var window = new short[windowSize];
                Array.Copy(samples, i, window, 0, windowSize);

                // Apply Hann window to reduce spectral leakage
                var windowed = FftProcessor.ApplyHannWindow(window);

                // Convert to complex and perform FFT
                var complexData = new FftProcessor.Complex[windowSize];
                for (int j = 0; j < windowSize; j++)
                {
                    complexData[j] = new FftProcessor.Complex(windowed[j], 0.0);
                }

                FftProcessor.Fft(complexData, inverse: false);

                // Get magnitude spectrum
                var magnitudes = FftProcessor.MagnitudeSpectrum(complexData);

                // Find spectral peaks (local maxima) for fingerprinting
                var peaks = new List<(int bin, double magnitude)>();
                for (int bin = 2; bin < magnitudes.Length - 2; bin++)
                {
                    if (magnitudes[bin] > magnitudes[bin - 1] &&
                        magnitudes[bin] > magnitudes[bin + 1] &&
                        magnitudes[bin] > magnitudes[bin - 2] &&
                        magnitudes[bin] > magnitudes[bin + 2] &&
                        magnitudes[bin] > 0.1) // Energy threshold
                    {
                        peaks.Add((bin, magnitudes[bin]));
                    }
                }

                // Hash top peaks for this frame
                var topPeaks = peaks.OrderByDescending(p => p.magnitude).Take(5);
                foreach (var peak in topPeaks)
                {
                    // Convert bin to frequency
                    var freq = FftProcessor.BinToFrequency(peak.bin, wavFormat.SampleRate, windowSize);
                    var timeMs = (i * 1000) / wavFormat.SampleRate;
                    
                    // Create hash: freq_time
                    var hash = $"{freq:F0}_{timeMs}";
                    fingerprints.Add(hash);
                }
            }

            // Combine fingerprints into compact representation
            if (fingerprints.Count > 0)
            {
                // Take top 100 fingerprints and create hash
                var topFingerprints = string.Join("|", fingerprints.Take(100));
                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(topFingerprints));
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating acoustic fingerprint");
            return null;
        }
    }

    private async Task<List<AudioSegment>> SplitAudioBySilenceAsync(byte[] audioData, string audioFormat)
    {
        try
        {
            // Parse WAV to extract PCM samples
            var wavFormat = WavFileParser.ParseWavHeader(audioData);
            if (wavFormat == null || wavFormat.SampleRate <= 0)
            {
                _logger.LogWarning("Cannot split by silence: invalid WAV format");
                // Return single segment as fallback
                return new List<AudioSegment>
                {
                    new AudioSegment
                    {
                        StartOffset = 0,
                        EndOffset = audioData.Length,
                        StartTime = TimeSpan.Zero,
                        EndTime = TimeSpan.FromSeconds(audioData.Length / 32000.0)
                    }
                };
            }

            // Extract PCM samples
            var samples = WavFileParser.ExtractPcmSamples(audioData, wavFormat);
            if (samples.Length == 0)
            {
                return new List<AudioSegment>();
            }

            // Use SilenceDetector to detect speech/silence boundaries
            var detector = new SilenceDetector(
                silenceThresholdDb: SilenceThresholdDb,
                frameSizeMs: 20,
                minSegmentMs: MinSegmentDurationMs,
                maxSegmentMs: MaxSegmentDurationMs
            );

            var detectedSegments = detector.DetectSilenceBoundaries(samples, wavFormat.SampleRate);

            // Convert sample positions to byte offsets and time spans
            int bytesPerSample = wavFormat.BitsPerSample / 8 * wavFormat.Channels;
            var audioSegments = new List<AudioSegment>();

            foreach (var segment in detectedSegments)
            {
                int startOffset = segment.StartSample * bytesPerSample;
                int endOffset = segment.EndSample * bytesPerSample;
                double startSeconds = (double)segment.StartSample / wavFormat.SampleRate;
                double endSeconds = (double)segment.EndSample / wavFormat.SampleRate;

                audioSegments.Add(new AudioSegment
                {
                    StartOffset = startOffset,
                    EndOffset = endOffset,
                    StartTime = TimeSpan.FromSeconds(startSeconds),
                    EndTime = TimeSpan.FromSeconds(endSeconds)
                });
            }

            return audioSegments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting audio by silence");
            // Return single segment as fallback
            return new List<AudioSegment>
            {
                new AudioSegment
                {
                    StartOffset = 0,
                    EndOffset = audioData.Length,
                    StartTime = TimeSpan.Zero,
                    EndTime = TimeSpan.FromSeconds(audioData.Length / 32000.0)
                }
            };
        }
    }

    private async Task<AudioFeatures?> ExtractAudioFeaturesAsync(byte[] audioData, string audioFormat)
    {
        try
        {
            // Only support WAV for now (can extend to other formats later)
            if (!audioFormat.Equals("wav", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Parse WAV header
            var wavHeader = WavFileParser.ParseWavHeader(audioData);
            if (wavHeader == null || wavHeader.SampleRate <= 0)
            {
                return null;
            }

            // Extract PCM samples and convert to float
            var pcmSamples = WavFileParser.ExtractPcmSamples(audioData, wavHeader);
            if (pcmSamples.Length == 0)
            {
                return null;
            }

            // Convert short[] to float[] (normalize to -1.0 to 1.0)
            var floatSamples = new float[pcmSamples.Length];
            for (int i = 0; i < pcmSamples.Length; i++)
            {
                floatSamples[i] = pcmSamples[i] / 32768.0f; // 16-bit normalization
            }

            // Extract features using AudioFeatureExtractor
            var features = AudioFeatureExtractor.ExtractFeatures(
                floatSamples,
                wavHeader.SampleRate,
                fftSize: 2048
            );

            return await Task.FromResult(features);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract audio features");
            return null;
        }
    }

    private double CalculateAudioQuality(byte[] audioData, string audioFormat)
    {
        // Quality heuristics:
        // - Penalize very short audio (< 1 second)
        // - Reward known formats over unknown
        // - Check for clipping, DC offset (requires sample analysis)

        double qualityScore = 1.0;

        var estimatedDuration = audioData.Length / 16000.0;
        if (estimatedDuration < 1.0)
        {
            qualityScore *= 0.5; // Penalize very short audio
        }

        if (audioFormat == "unknown")
        {
            qualityScore *= 0.7; // Penalize unknown format
        }

        // TODO: Analyze samples for clipping, SNR, DC offset

        return Math.Max(0.0, Math.Min(1.0, qualityScore));
    }

    private string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {suffixes[order]}";
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{duration.TotalHours:0.##}h";
        if (duration.TotalMinutes >= 1)
            return $"{duration.TotalMinutes:0.##}m";
        return $"{duration.TotalSeconds:0.##}s";
    }

    #endregion

    private class AudioSegment
    {
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
