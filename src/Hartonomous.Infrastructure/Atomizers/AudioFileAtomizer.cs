using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Audio;
using Hartonomous.Core.Models.Media;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Infrastructure.Services.Vision;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes complete audio files (MP3, FLAC, WAV, AAC, OGG) into sample atoms.
/// Extracts metadata (ID3 tags, format info), decodes to PCM samples, and delegates to AudioStreamAtomizer.
/// Creates hierarchical structure: Audio Parent (with JSON metadata) → Sample Atoms.
/// </summary>
public class AudioFileAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    private readonly AudioStreamAtomizer _audioStreamAtomizer;

    public int Priority => 25;

    public AudioFileAtomizer()
    {
        _audioStreamAtomizer = new AudioStreamAtomizer();
    }

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.StartsWith("audio/") == true)
            return true;

        var audioExtensions = new[] { "mp3", "flac", "wav", "aac", "ogg", "m4a", "wma", "opus", "aiff" };
        return fileExtension != null && audioExtensions.Contains(fileExtension.ToLowerInvariant());
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Extract audio metadata
            AudioMetadata? audioMetadata = null;
            try
            {
                audioMetadata = AudioMetadataExtractor.ExtractMetadata(input);
            }
            catch (Exception ex)
            {
                warnings.Add($"Audio metadata extraction failed: {ex.Message}");
            }

            // Analyze compression
            CompressionMetrics? compressionMetrics = null;
            if (audioMetadata != null)
            {
                try
                {
                    compressionMetrics = CompressionAnalyzer.AnalyzeAudio(audioMetadata);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Compression analysis failed: {ex.Message}");
                }
            }

            // Create audio parent atom with JSON metadata
            var audioHash = SHA256.HashData(input);
            var audioMetadataBytes = Encoding.UTF8.GetBytes($"audio:{source.FileName}:{audioMetadata?.Format ?? "unknown"}");

            string metadataJson;
            if (audioMetadata != null)
            {
                metadataJson = MetadataJsonSerializer.SerializeAudioMetadata(audioMetadata, compressionMetrics);
            }
            else
            {
                // Fallback minimal metadata
                metadataJson = $"{{\"mediaType\":\"audio\",\"format\":\"unknown\",\"fileSizeBytes\":{input.Length}}}";
            }

            var audioAtom = new AtomData
            {
                AtomicValue = audioMetadataBytes.Length <= MaxAtomSize ? audioMetadataBytes : audioMetadataBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = audioHash,
                Modality = "audio",
                Subtype = "audio-metadata",
                ContentType = source.ContentType,
                CanonicalText = audioMetadata != null
                    ? $"{audioMetadata.Title ?? source.FileName ?? "audio"} ({audioMetadata.DurationSeconds:F1}s, {audioMetadata.Format})"
                    : $"{source.FileName ?? "audio"}",
                Metadata = metadataJson
            };
            atoms.Add(audioAtom);

            // Decode audio to PCM using FFmpeg
            if (audioMetadata != null)
            {
                try
                {
                    // Decode to PCM WAV
                    var pcmData = await FFmpegHelper.DecodeAudioToPCMAsync(input, cancellationToken);

                    var sampleRate = audioMetadata.SampleRate;
                    var channels = audioMetadata.Channels;
                    var bytesPerSample = 2; // 16-bit PCM
                    var samplesPerSecond = sampleRate * channels;
                    var bytesPerSecond = samplesPerSecond * bytesPerSample;

                    // Skip WAV header (44 bytes)
                    var audioStart = 44;
                    var bufferIndex = 0;
                    var baseTimestamp = DateTime.UtcNow;

                    for (var offset = audioStart; offset < pcmData.Length; offset += bytesPerSecond)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var bufferSize = Math.Min(bytesPerSecond, pcmData.Length - offset);
                        var bufferSamples = new byte[bufferSize];
                        Array.Copy(pcmData, offset, bufferSamples, 0, bufferSize);

                        var audioBuffer = new AudioBuffer
                        {
                            StreamId = source.FileName ?? "audio-file",
                            BufferId = $"{source.FileName}_buffer_{bufferIndex:D6}",
                            SampleRate = sampleRate,
                            Channels = channels,
                            BitsPerSample = 16,
                            SampleCount = bufferSize / (channels * bytesPerSample),
                            Samples = bufferSamples,
                            Timestamp = baseTimestamp.AddSeconds(bufferIndex)
                        };

                        var bufferSource = new SourceMetadata
                        {
                            FileName = $"{source.FileName}_buffer_{bufferIndex:D6}",
                            ContentType = "audio/x-raw-int16",
                            SourceUri = $"{source.SourceUri}#t={bufferIndex}",
                            TenantId = source.TenantId,
                            SourceType = "audio-buffer"
                        };

                        var bufferResult = await _audioStreamAtomizer.AtomizeAsync(audioBuffer, bufferSource, cancellationToken);

                        // Add buffer atoms
                        atoms.AddRange(bufferResult.Atoms);

                        // Link buffer atom to audio parent
                        var bufferAtom = bufferResult.Atoms.FirstOrDefault(a => a.Subtype == "buffer-id");
                        if (bufferAtom != null)
                        {
                            compositions.Add(new AtomComposition
                            {
                                ParentAtomHash = audioHash,
                                ComponentAtomHash = bufferAtom.ContentHash,
                                SequenceIndex = bufferIndex,
                                Position = new SpatialPosition
                                {
                                    X = 0,
                                    Y = 0,
                                    Z = 0,
                                    M = bufferIndex // Temporal offset in seconds
                                }
                            });
                        }

                        // Add buffer's internal compositions (samples)
                        compositions.AddRange(bufferResult.Compositions);

                        bufferIndex++;
                    }

                    if (bufferIndex > 0)
                    {
                        warnings.Add($"Extracted {bufferIndex} audio buffers ({pcmData.Length / 1024.0:F1} KB PCM data)");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"PCM sample extraction failed: {ex.Message}");
                }
            }

            // Note: Legacy audio decoding pattern removed - using FFmpeg-based implementation above
            /*
            if (audioMetadata != null)
            {
                // Decode audio file to PCM using NAudio or FFmpeg
                byte[] pcmData = await DecodeAudioToPCM(input, audioMetadata.Format);
                
                var sampleRate = audioMetadata.SampleRate ?? 44100;
                var channels = audioMetadata.Channels ?? 2;
                var bitsPerSample = audioMetadata.BitDepth ?? 16;
                var bytesPerSample = bitsPerSample / 8;
                var samplesPerSecond = sampleRate * channels;
                var bytesPerSecond = samplesPerSecond * bytesPerSample;
                
                var bufferIndex = 0;
                var baseTimestamp = DateTime.UtcNow;
                
                for (int offset = 0; offset < pcmData.Length; offset += bytesPerSecond)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var bufferSize = Math.Min(bytesPerSecond, pcmData.Length - offset);
                    var bufferSamples = new byte[bufferSize];
                    Array.Copy(pcmData, offset, bufferSamples, 0, bufferSize);
                    
                    var audioBuffer = new AudioBuffer
                    {
                        StreamId = source.FileName ?? "audio-file",
                        BufferId = $"{source.FileName}_buffer_{bufferIndex:D6}",
                        SampleRate = sampleRate,
                        Channels = channels,
                        BitsPerSample = bitsPerSample,
                        SampleCount = bufferSize / (channels * bytesPerSample),
                        Samples = bufferSamples,
                        Timestamp = baseTimestamp.AddSeconds(bufferIndex)
                    };
                    
                    var bufferSource = new SourceMetadata
                    {
                        FileName = $"{source.FileName}_buffer_{bufferIndex:D6}",
                        ContentType = $"audio/x-raw-int{bitsPerSample}",
                        SourceUri = $"{source.SourceUri}#t={bufferIndex}",
                        TenantId = source.TenantId,
                        SourceType = "audio-buffer"
                    };
                    
                    var bufferResult = await _audioStreamAtomizer.AtomizeAsync(audioBuffer, bufferSource, cancellationToken);
                    
                    // Add buffer atoms
                    atoms.AddRange(bufferResult.Atoms);
                    
                    // Link buffer atom to audio parent
                    var bufferAtom = bufferResult.Atoms.FirstOrDefault(a => a.Subtype == "buffer-id");
                    if (bufferAtom != null)
                    {
                        compositions.Add(new AtomComposition
                        {
                            ParentAtomHash = audioHash,
                            ComponentAtomHash = bufferAtom.ContentHash,
                            SequenceIndex = bufferIndex,
                            Position = new SpatialPosition
                            {
                                X = 0,
                                Y = 0,
                                Z = 0,
                                M = bufferIndex // Temporal offset in seconds
                            }
                        });
                    }
                    
                    // Add buffer's internal compositions (samples)
                    compositions.AddRange(bufferResult.Compositions);
                    
                    bufferIndex++;
                }
                
                warnings.Add($"Extracted {bufferIndex} audio buffers ({pcmData.Length / 1024.0:F1} KB PCM data)");
            }
            */

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count(),
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(AudioFileAtomizer),
                    DetectedFormat = audioMetadata != null
                        ? $"{audioMetadata.Format} ({audioMetadata.SampleRate}Hz, {audioMetadata.Channels}ch, {audioMetadata.Codec})"
                        : "Unknown audio format",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Audio file atomization failed: {ex.Message}");
            throw;
        }
    }

    // Note: DecodeAudioToPCM functionality now handled by FFmpegHelper.ExtractAudioAsPcm
}
