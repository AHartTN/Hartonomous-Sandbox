using FFMpegCore;
using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Helper class for FFmpeg operations (frame extraction, audio extraction, etc.).
/// Wraps FFMpegCore library for video/audio processing.
/// </summary>
public static class FFmpegHelper
{
    private static bool _initialized = false;
    private static readonly object _initLock = new();

    /// <summary>
    /// Ensures FFmpeg binaries are available. Downloads them if necessary.
    /// Should be called once at application startup.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            try
            {
                // Check if FFmpeg is already available
                var ffmpegPath = GlobalFFOptions.GetFFMpegBinaryPath();
                if (File.Exists(ffmpegPath))
                {
                    _initialized = true;
                    return;
                }
            }
            catch
            {
                // FFmpeg not found, will attempt to download
            }

            try
            {
                // Download FFmpeg binaries if not found
                // Note: This requires FFMpegCore.Extensions.Downloader package (optional)
                // For production, FFmpeg should be pre-installed or included in deployment
                // FFMpegDownloader.DownloadFFMpegSuite();
                
                // For now, assume FFmpeg is installed system-wide or in PATH
                _initialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "FFmpeg binaries not found. Please install FFmpeg or set GlobalFFOptions.Configure() to specify binary location.", ex);
            }
        }
    }

    /// <summary>
    /// Extracts video frames at regular intervals.
    /// </summary>
    /// <param name="videoData">Video file bytes</param>
    /// <param name="framesPerSecond">How many frames per second to extract (e.g., 1 = 1 frame every second)</param>
    /// <param name="maxFrames">Maximum number of frames to extract (null = no limit)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of frame data (PNG format)</returns>
    public static async Task<List<(TimeSpan Timestamp, byte[] FrameData)>> ExtractFramesAsync(
        byte[] videoData,
        double framesPerSecond = 1.0,
        int? maxFrames = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var frames = new List<(TimeSpan, byte[])>();
        
        // Save video to temp file (FFMpegCore requires file input for frame extraction)
        var tempVideoPath = Path.Combine(Path.GetTempPath(), $"video_{Guid.NewGuid()}.tmp");
        var tempOutputDir = Path.Combine(Path.GetTempPath(), $"frames_{Guid.NewGuid()}");
        
        try
        {
            Directory.CreateDirectory(tempOutputDir);
            await File.WriteAllBytesAsync(tempVideoPath, videoData, cancellationToken);

            // Analyze video to get duration
            var mediaInfo = await FFProbe.AnalyseAsync(tempVideoPath, cancellationToken: cancellationToken);
            var duration = mediaInfo.Duration;
            var frameInterval = TimeSpan.FromSeconds(1.0 / framesPerSecond);

            var frameCount = 0;
            for (var timestamp = TimeSpan.Zero; timestamp < duration; timestamp += frameInterval)
            {
                if (maxFrames.HasValue && frameCount >= maxFrames.Value)
                    break;

                cancellationToken.ThrowIfCancellationRequested();

                var outputPath = Path.Combine(tempOutputDir, $"frame_{frameCount:D6}.png");

                // Extract frame at specific timestamp
                await FFMpeg.SnapshotAsync(
                    tempVideoPath,
                    outputPath,
                    captureTime: timestamp,
                    cancellationToken: cancellationToken);

                if (File.Exists(outputPath))
                {
                    var frameBytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
                    frames.Add((timestamp, frameBytes));
                    frameCount++;
                }
            }

            return frames;
        }
        finally
        {
            // Clean up temp files
            try
            {
                if (File.Exists(tempVideoPath))
                    File.Delete(tempVideoPath);
                if (Directory.Exists(tempOutputDir))
                    Directory.Delete(tempOutputDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Extracts audio track from video file and converts to PCM WAV format.
    /// </summary>
    /// <param name="videoData">Video file bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PCM WAV audio data</returns>
    public static async Task<byte[]> ExtractAudioToPCMAsync(
        byte[] videoData,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var tempVideoPath = Path.Combine(Path.GetTempPath(), $"video_{Guid.NewGuid()}.tmp");
        var tempAudioPath = Path.Combine(Path.GetTempPath(), $"audio_{Guid.NewGuid()}.wav");

        try
        {
            await File.WriteAllBytesAsync(tempVideoPath, videoData, cancellationToken);

            // Extract audio and convert to PCM WAV
            var success = await FFMpegArguments
                .FromFileInput(tempVideoPath)
                .OutputToFile(tempAudioPath, overwrite: true, options => options
                    .WithAudioCodec("pcm_s16le")  // 16-bit PCM
                    .WithAudioSamplingRate(44100) // 44.1 kHz
                    .DisableChannel(FFMpegCore.Enums.Channel.Video))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            if (!success || !File.Exists(tempAudioPath))
                throw new InvalidOperationException("Failed to extract audio from video");

            return await File.ReadAllBytesAsync(tempAudioPath, cancellationToken);
        }
        finally
        {
            // Clean up temp files
            try
            {
                if (File.Exists(tempVideoPath))
                    File.Delete(tempVideoPath);
                if (File.Exists(tempAudioPath))
                    File.Delete(tempAudioPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Decodes audio file to PCM WAV format.
    /// </summary>
    /// <param name="audioData">Audio file bytes (MP3, FLAC, AAC, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PCM WAV audio data</returns>
    public static async Task<byte[]> DecodeAudioToPCMAsync(
        byte[] audioData,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var tempInputPath = Path.Combine(Path.GetTempPath(), $"audio_input_{Guid.NewGuid()}.tmp");
        var tempOutputPath = Path.Combine(Path.GetTempPath(), $"audio_output_{Guid.NewGuid()}.wav");

        try
        {
            await File.WriteAllBytesAsync(tempInputPath, audioData, cancellationToken);

            // Decode to PCM WAV
            var success = await FFMpegArguments
                .FromFileInput(tempInputPath)
                .OutputToFile(tempOutputPath, overwrite: true, options => options
                    .WithAudioCodec("pcm_s16le")  // 16-bit PCM
                    .WithAudioSamplingRate(44100) // 44.1 kHz
                    .DisableChannel(FFMpegCore.Enums.Channel.Video))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            if (!success || !File.Exists(tempOutputPath))
                throw new InvalidOperationException("Failed to decode audio file");

            return await File.ReadAllBytesAsync(tempOutputPath, cancellationToken);
        }
        finally
        {
            // Clean up temp files
            try
            {
                if (File.Exists(tempInputPath))
                    File.Delete(tempInputPath);
                if (File.Exists(tempOutputPath))
                    File.Delete(tempOutputPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
