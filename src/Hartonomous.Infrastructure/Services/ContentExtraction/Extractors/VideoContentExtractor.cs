using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services.ContentExtraction;

namespace Hartonomous.Infrastructure.Services.ContentExtraction.Extractors;

/// <summary>
/// Extracts content from video files (MP4, MKV, AVI, etc.).
/// Decomposes videos into keyframes (image atoms), audio tracks (audio atoms), and metadata.
/// </summary>
public sealed class VideoContentExtractor : IContentExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v"
    };

    public bool CanHandle(ContentExtractionContext context)
    {
        if (context.SourceType != ContentSourceType.Stream || context.ContentStream == null)
        {
            return false;
        }

        var extension = context.Extension ?? "";
        return SupportedExtensions.Contains(extension);
    }

    public async Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken)
    {
        var fileName = context.FileName ?? "unknown";
        var sourceUri = context.Metadata.TryGetValue("sourceUri", out var uri) ? uri : $"file:///{fileName}";

        var requests = new List<AtomIngestionRequest>();
        var diagnostics = new Dictionary<string, string>();

        diagnostics["file_name"] = fileName;
        diagnostics["source_uri"] = sourceUri;

        try
        {
            // Save stream to temp file (FFmpeg requires file path)
            var tempVideoPath = Path.Combine(Path.GetTempPath(), $"video_{Guid.NewGuid()}{context.Extension}");
            
            await using (var fileStream = File.Create(tempVideoPath))
            {
                context.ContentStream!.Position = 0;
                await context.ContentStream.CopyToAsync(fileStream, cancellationToken);
            }

            try
            {
                var videoInfo = await FFProbe.AnalyseAsync(tempVideoPath, cancellationToken: cancellationToken);
                
                diagnostics["duration_seconds"] = videoInfo.Duration.TotalSeconds.ToString("F2");
                diagnostics["format"] = videoInfo.Format.FormatName;
                diagnostics["bitrate"] = videoInfo.Format.BitRate.ToString();
                diagnostics["video_streams"] = videoInfo.VideoStreams.Count.ToString();
                diagnostics["audio_streams"] = videoInfo.AudioStreams.Count.ToString();

                // Extract video metadata atom
                var videoMetadata = new MetadataEnvelope()
                    .Set("duration_seconds", videoInfo.Duration.TotalSeconds)
                    .Set("format", videoInfo.Format.FormatName)
                    .Set("bitrate", videoInfo.Format.BitRate);

                if (videoInfo.VideoStreams.Any())
                {
                    var videoStream = videoInfo.VideoStreams.First();
                    videoMetadata
                        .Set("width", videoStream.Width)
                        .Set("height", videoStream.Height)
                        .Set("fps", videoStream.FrameRate)
                        .Set("codec", videoStream.CodecName);

                    diagnostics["width"] = videoStream.Width.ToString();
                    diagnostics["height"] = videoStream.Height.ToString();
                    diagnostics["fps"] = videoStream.FrameRate.ToString("F2");
                }

                var videoAtom = new AtomIngestionRequestBuilder()
                    .WithCanonicalText($"Video: {fileName} ({videoInfo.Duration.TotalSeconds:F1}s)")
                    .WithModality("video", "metadata")
                    .WithSource("video_extractor", sourceUri)
                    .WithMetadata(videoMetadata)
                    .Build();

                requests.Add(videoAtom);

                // Extract keyframes
                var keyframeCount = await ExtractKeyframes(tempVideoPath, videoInfo, requests, sourceUri, diagnostics, cancellationToken);
                diagnostics["keyframes_extracted"] = keyframeCount.ToString();

                // Extract audio
                if (videoInfo.AudioStreams.Any())
                {
                    var audioExtracted = await ExtractAudio(tempVideoPath, videoInfo, requests, sourceUri, diagnostics, cancellationToken);
                    diagnostics["audio_extracted"] = audioExtracted ? "true" : "false";
                }

                diagnostics["extraction_status"] = "success";
                diagnostics["atoms_created"] = requests.Count.ToString();
            }
            finally
            {
                // Cleanup temp file
                if (File.Exists(tempVideoPath))
                {
                    File.Delete(tempVideoPath);
                }
            }
        }
        catch (Exception ex)
        {
            diagnostics["extraction_status"] = "failed";
            diagnostics["error"] = ex.Message;
        }

        return new ContentExtractionResult(requests, diagnostics);
    }

    private async Task<int> ExtractKeyframes(
        string videoPath,
        IMediaAnalysis videoInfo,
        List<AtomIngestionRequest> requests,
        string sourceUri,
        Dictionary<string, string> diagnostics,
        CancellationToken cancellationToken)
    {
        var duration = videoInfo.Duration.TotalSeconds;
        var fps = videoInfo.VideoStreams.FirstOrDefault()?.FrameRate ?? 30;
        
        // Extract keyframes every 5 seconds (or every 10% of video, whichever is larger)
        var interval = Math.Max(5.0, duration / 10);
        var frameCount = 0;

        var outputDir = Path.Combine(Path.GetTempPath(), $"frames_{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDir);

        try
        {
            var timestamps = new List<double>();
            for (double t = 0; t < duration; t += interval)
            {
                timestamps.Add(t);
            }

            // Extract frames
            for (int i = 0; i < timestamps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var timestamp = TimeSpan.FromSeconds(timestamps[i]);
                var outputPath = Path.Combine(outputDir, $"frame_{i:D4}.jpg");

                await FFMpeg.SnapshotAsync(videoPath, outputPath, null, timestamp);

                if (File.Exists(outputPath))
                {
                    var frameBytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
                    var frameBase64 = Convert.ToBase64String(frameBytes);

                    var frameMetadata = new MetadataEnvelope()
                        .Set("timestamp_seconds", timestamps[i])
                        .Set("frame_index", i)
                        .Set("video_source", sourceUri);

                    var frameAtom = new AtomIngestionRequestBuilder()
                        .WithCanonicalText($"Frame at {timestamps[i]:F1}s")
                        .WithModality("image", "video_frame")
                        .WithSource("video_extractor", sourceUri)
                        .WithPayloadLocator(outputPath) // Will be replaced with actual storage location
                        .WithMetadata(frameMetadata)
                        .Build();

                    requests.Add(frameAtom);
                    frameCount++;

                    File.Delete(outputPath);
                }
            }
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }

        return frameCount;
    }

    private async Task<bool> ExtractAudio(
        string videoPath,
        IMediaAnalysis videoInfo,
        List<AtomIngestionRequest> requests,
        string sourceUri,
        Dictionary<string, string> diagnostics,
        CancellationToken cancellationToken)
    {
        var audioStream = videoInfo.AudioStreams.FirstOrDefault();
        if (audioStream == null)
        {
            return false;
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"audio_{Guid.NewGuid()}.mp3");

        try
        {
            // Use FFMpegArguments for audio extraction
            await FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(outputPath, true, options => options
                    .WithAudioCodec(AudioCodec.LibMp3Lame)
                    .WithAudioBitrate(128))
                .ProcessAsynchronously();

            if (File.Exists(outputPath))
            {
                var audioBytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
                
                var audioMetadata = new MetadataEnvelope()
                    .Set("codec", audioStream.CodecName)
                    .Set("sample_rate", audioStream.SampleRateHz)
                    .Set("channels", audioStream.Channels)
                    .Set("bitrate", audioStream.BitRate)
                    .Set("duration_seconds", audioStream.Duration.TotalSeconds)
                    .Set("video_source", sourceUri);

                var audioAtom = new AtomIngestionRequestBuilder()
                    .WithCanonicalText($"Audio track ({audioStream.Duration.TotalSeconds:F1}s)")
                    .WithModality("audio", "video_audio")
                    .WithSource("video_extractor", sourceUri)
                    .WithPayloadLocator(outputPath) // Will be replaced with actual storage location
                    .WithMetadata(audioMetadata)
                    .Build();

                requests.Add(audioAtom);

                diagnostics["audio_codec"] = audioStream.CodecName;
                diagnostics["audio_sample_rate"] = audioStream.SampleRateHz.ToString();
                diagnostics["audio_channels"] = audioStream.Channels.ToString();

                return true;
            }
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }

        return false;
    }
}
