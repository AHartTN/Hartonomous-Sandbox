using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.Services;
using Hartonomous.Infrastructure.Services.Vision;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes complete video files (MP4, AVI, MOV, MKV, WebM) by delegating to specialized atomizers.
/// Extracts metadata, delegates frame extraction to ImageAtomizer, and audio extraction to AudioStreamAtomizer.
/// Creates hierarchical atom structure: Video Parent → Frames (pixels via ImageAtomizer) → Audio (samples via AudioStreamAtomizer).
/// </summary>
public class VideoFileAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    private readonly ImageAtomizer _imageAtomizer;
    private readonly AudioStreamAtomizer _audioStreamAtomizer;

    public int Priority => 30;

    public VideoFileAtomizer()
    {
        _imageAtomizer = new ImageAtomizer();
        _audioStreamAtomizer = new AudioStreamAtomizer();
    }

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.StartsWith("video/") == true)
            return true;

        var videoExtensions = new[] { "mp4", "avi", "mov", "mkv", "webm", "flv", "wmv", "m4v" };
        return fileExtension != null && videoExtensions.Contains(fileExtension.ToLowerInvariant());
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Extract video metadata
            VideoMetadata? videoMetadata = null;
            try
            {
                videoMetadata = VideoMetadataExtractor.ExtractMetadata(input);
            }
            catch (Exception ex)
            {
                warnings.Add($"Video metadata extraction failed: {ex.Message}");
            }

            // Extract audio metadata (if video has audio track)
            AudioMetadata? audioMetadata = null;
            if (videoMetadata?.HasAudio == true)
            {
                try
                {
                    audioMetadata = AudioMetadataExtractor.ExtractMetadata(input);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Audio metadata extraction from video failed: {ex.Message}");
                }
            }

            // Analyze compression
            CompressionMetrics? compressionMetrics = null;
            if (videoMetadata != null)
            {
                try
                {
                    compressionMetrics = CompressionAnalyzer.AnalyzeVideo(videoMetadata);
                }
                catch (Exception ex)
                {
                    warnings.Add($"Compression analysis failed: {ex.Message}");
                }
            }

            // Create video parent atom with JSON metadata
            var videoHash = SHA256.HashData(input);
            var videoMetadataBytes = Encoding.UTF8.GetBytes($"video:{source.FileName}:{videoMetadata?.Width ?? 0}x{videoMetadata?.Height ?? 0}");

            string metadataJson;
            if (videoMetadata != null)
            {
                metadataJson = MetadataJsonSerializer.SerializeVideoMetadata(videoMetadata, compressionMetrics);
            }
            else
            {
                // Fallback minimal metadata
                metadataJson = $"{{\"mediaType\":\"video\",\"format\":\"unknown\",\"fileSizeBytes\":{input.Length}}}";
            }

            var videoAtom = new AtomData
            {
                AtomicValue = videoMetadataBytes.Length <= MaxAtomSize ? videoMetadataBytes : videoMetadataBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = videoHash,
                Modality = "video",
                Subtype = "video-metadata",
                ContentType = source.ContentType,
                CanonicalText = $"{source.FileName ?? "video"} ({videoMetadata?.Width ?? 0}×{videoMetadata?.Height ?? 0}, {videoMetadata?.DurationSeconds ?? 0:F1}s)",
                Metadata = metadataJson
            };
            atoms.Add(videoAtom);

            // Extract video frames using FFmpeg
            if (videoMetadata != null)
            {
                try
                {
                    // Extract frames at 1 fps (configurable)
                    var frames = await FFmpegHelper.ExtractFramesAsync(
                        input,
                        framesPerSecond: 1.0,
                        maxFrames: 60, // Limit to first 60 seconds for performance
                        cancellationToken: cancellationToken);

                    var frameIndex = 0;
                    foreach (var (timestamp, frameBytes) in frames)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var frameSource = new SourceMetadata
                        {
                            FileName = $"{source.FileName}_frame_{frameIndex:D6}.png",
                            ContentType = "image/png",
                            SourceUri = $"{source.SourceUri}#t={timestamp.TotalSeconds:F3}",
                            TenantId = source.TenantId,
                            SourceType = "video-frame"
                        };

                        // Delegate frame atomization to ImageAtomizer
                        var frameResult = await _imageAtomizer.AtomizeAsync(frameBytes, frameSource, cancellationToken);

                        // Add frame atoms
                        atoms.AddRange(frameResult.Atoms);

                        // Link frame parent atom to video parent atom
                        var frameParentAtom = frameResult.Atoms.FirstOrDefault(a => a.Subtype == "image-metadata");
                        if (frameParentAtom != null)
                        {
                            compositions.Add(new AtomComposition
                            {
                                ParentAtomHash = videoHash,
                                ComponentAtomHash = frameParentAtom.ContentHash,
                                SequenceIndex = frameIndex,
                                Position = new SpatialPosition
                                {
                                    X = 0,
                                    Y = 0,
                                    Z = 0,
                                    M = timestamp.TotalSeconds // Temporal coordinate
                                }
                            });
                        }

                        // Add frame's internal compositions (pixels)
                        compositions.AddRange(frameResult.Compositions);

                        frameIndex++;
                    }

                    if (frames.Count > 0)
                    {
                        warnings.Add($"Extracted {frames.Count} video frames at 1 fps");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Frame extraction failed: {ex.Message}");
                }
            }

            // Note: Legacy frame extraction pattern removed - using FFmpeg-based implementation above
            /*
            var frameCount = 0;
            var frameInterval = 1.0; // Extract 1 frame per second
            
            for (double timestamp = 0; timestamp < (videoMetadata?.DurationSeconds ?? 0); timestamp += frameInterval)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Extract frame at timestamp using FFmpeg
                byte[] frameBytes = await ExtractFrameAtTimestamp(input, timestamp);
                
                // Delegate pixel atomization to ImageAtomizer
                var frameSource = new SourceMetadata
                {
                    FileName = $"{source.FileName}_frame_{frameCount:D6}.png",
                    ContentType = "image/png",
                    SourceUri = $"{source.SourceUri}#t={timestamp}",
                    TenantId = source.TenantId,
                    SourceType = "video-frame"
                };
                
                var frameResult = await _imageAtomizer.AtomizeAsync(frameBytes, frameSource, cancellationToken);
                
                // Add frame atoms to collection
                atoms.AddRange(frameResult.Atoms);
                
                // Link frame parent atom to video parent
                var frameParentAtom = frameResult.Atoms.FirstOrDefault(a => a.Subtype == "image-metadata");
                if (frameParentAtom != null)
                {
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = videoHash,
                        ComponentAtomHash = frameParentAtom.ContentHash,
                        SequenceIndex = frameCount,
                        Position = new SpatialPosition
                        {
                            X = 0, // Frame index could go here
                            Y = 0,
                            Z = 0,
                            M = timestamp // Temporal coordinate
                        }
                    });
                }
                
                // Add frame's internal compositions (pixels)
                compositions.AddRange(frameResult.Compositions);
                
                frameCount++;
            }
            */

            // Extract audio track using FFmpeg
            if (audioMetadata != null && videoMetadata != null)
            {
                try
                {
                    // Extract audio as PCM WAV
                    var pcmData = await FFmpegHelper.ExtractAudioToPCMAsync(input, cancellationToken);

                    // Calculate buffer parameters
                    var sampleRate = audioMetadata.SampleRate;
                    var channels = audioMetadata.Channels;
                    var bytesPerSample = 2; // 16-bit PCM
                    var samplesPerSecond = sampleRate * channels;
                    var bytesPerSecond = samplesPerSecond * bytesPerSample;

                    // Skip WAV header (44 bytes)
                    var audioStart = 44;
                    var bufferIndex = 0;

                    for (var offset = audioStart; offset < pcmData.Length; offset += bytesPerSecond)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var bufferSize = Math.Min(bytesPerSecond, pcmData.Length - offset);
                        var bufferSamples = new byte[bufferSize];
                        Array.Copy(pcmData, offset, bufferSamples, 0, bufferSize);

                        var audioBuffer = new AudioBuffer
                        {
                            StreamId = source.FileName ?? "video-audio",
                            BufferId = $"{source.FileName}_audio_buffer_{bufferIndex:D6}",
                            SampleRate = sampleRate,
                            Channels = channels,
                            BitsPerSample = 16,
                            SampleCount = bufferSize / (channels * bytesPerSample),
                            Samples = bufferSamples,
                            Timestamp = DateTime.UtcNow.AddSeconds(bufferIndex)
                        };

                        var bufferSource = new SourceMetadata
                        {
                            FileName = $"{source.FileName}_audio_buffer_{bufferIndex:D6}",
                            ContentType = "audio/x-raw-int16",
                            SourceUri = $"{source.SourceUri}#t={bufferIndex}",
                            TenantId = source.TenantId,
                            SourceType = "audio-buffer"
                        };

                        // Delegate buffer atomization to AudioStreamAtomizer
                        var bufferResult = await _audioStreamAtomizer.AtomizeAsync(audioBuffer, bufferSource, cancellationToken);

                        // Add buffer atoms
                        atoms.AddRange(bufferResult.Atoms);

                        // Link buffer atom to video parent
                        var bufferAtom = bufferResult.Atoms.FirstOrDefault(a => a.Subtype == "buffer-id");
                        if (bufferAtom != null)
                        {
                            compositions.Add(new AtomComposition
                            {
                                ParentAtomHash = videoHash,
                                ComponentAtomHash = bufferAtom.ContentHash,
                                SequenceIndex = bufferIndex,
                                Position = new SpatialPosition
                                {
                                    X = 0,
                                    Y = 0,
                                    Z = 1, // Audio layer
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
                    warnings.Add($"Audio extraction failed: {ex.Message}");
                }
            }

            // Note: Legacy audio extraction pattern removed - using FFmpeg-based implementation above
            /*
            if (videoMetadata?.HasAudio == true && audioMetadata != null)
            {
                // Extract audio track as PCM using FFmpeg
                byte[] audioPcmData = await ExtractAudioTrackAsPCM(input);
                
                // Split into 1-second buffers
                var sampleRate = audioMetadata.SampleRate ?? 44100;
                var channels = audioMetadata.Channels ?? 2;
                var bitsPerSample = audioMetadata.BitDepth ?? 16;
                var bytesPerSample = bitsPerSample / 8;
                var samplesPerSecond = sampleRate * channels;
                var bytesPerSecond = samplesPerSecond * bytesPerSample;
                
                var bufferIndex = 0;
                for (int offset = 0; offset < audioPcmData.Length; offset += bytesPerSecond)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var bufferSize = Math.Min(bytesPerSecond, audioPcmData.Length - offset);
                    var bufferSamples = new byte[bufferSize];
                    Array.Copy(audioPcmData, offset, bufferSamples, 0, bufferSize);
                    
                    var audioBuffer = new AudioBuffer
                    {
                        StreamId = source.FileName ?? "video-audio",
                        BufferId = $"{source.FileName}_audio_{bufferIndex:D6}",
                        SampleRate = sampleRate,
                        Channels = channels,
                        BitsPerSample = bitsPerSample,
                        SampleCount = bufferSize / (channels * bytesPerSample),
                        Samples = bufferSamples,
                        Timestamp = DateTime.UtcNow.AddSeconds(bufferIndex)
                    };
                    
                    var audioSource = new SourceMetadata
                    {
                        FileName = $"{source.FileName}_audio_{bufferIndex:D6}",
                        ContentType = $"audio/x-raw-int{bitsPerSample}",
                        SourceUri = $"{source.SourceUri}#audio",
                        TenantId = source.TenantId,
                        SourceType = "video-audio"
                    };
                    
                    var audioResult = await _audioStreamAtomizer.AtomizeAsync(audioBuffer, audioSource, cancellationToken);
                    
                    // Add audio atoms
                    atoms.AddRange(audioResult.Atoms);
                    
                    // Link audio buffer atom to video parent
                    var audioBufferAtom = audioResult.Atoms.FirstOrDefault(a => a.Subtype == "buffer-id");
                    if (audioBufferAtom != null)
                    {
                        compositions.Add(new AtomComposition
                        {
                            ParentAtomHash = videoHash,
                            ComponentAtomHash = audioBufferAtom.ContentHash,
                            SequenceIndex = bufferIndex,
                            Position = new SpatialPosition
                            {
                                X = 0,
                                Y = 0,
                                Z = 1, // Z=1 for audio layer (vs Z=0 for video frames)
                                M = bufferIndex // Temporal offset in seconds
                            }
                        });
                    }
                    
                    // Add audio's internal compositions (samples)
                    compositions.AddRange(audioResult.Compositions);
                    
                    bufferIndex++;
                }
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
                    AtomizerType = nameof(VideoFileAtomizer),
                    DetectedFormat = videoMetadata != null 
                        ? $"{videoMetadata.Container ?? videoMetadata.Format} ({videoMetadata.Width}×{videoMetadata.Height}, {videoMetadata.VideoCodec})"
                        : "Unknown video format",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Video file atomization failed: {ex.Message}");
            throw;
        }
    }

    // Note: Frame and audio extraction are handled by FFmpegHelper methods above
}
