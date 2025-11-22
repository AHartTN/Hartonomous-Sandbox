using System;
using Hartonomous.Core.Models.Media;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Analyze compression characteristics of media files.
/// Calculates compression ratio, estimates quality, detects lossy vs lossless.
/// </summary>
public static class CompressionAnalyzer
{
    public static CompressionMetrics AnalyzeImage(ImageMetadata imageMetadata, byte[] rawPixelData)
    {
        var metrics = new CompressionMetrics
        {
            MediaType = "Image",
            Format = imageMetadata.Format,
            CompressedSizeBytes = imageMetadata.FileSizeBytes
        };
        
        // Calculate uncompressed size
        var pixelCount = imageMetadata.Width * imageMetadata.Height;
        var bytesPerPixel = imageMetadata.ColorSpace switch
        {
            "Grayscale" => 1,
            "RGB" => 3,
            "RGBA" => 4,
            _ => imageMetadata.BitDepth / 8
        };
        
        metrics.UncompressedSizeBytes = pixelCount * bytesPerPixel;
        
        // Calculate compression ratio
        if (metrics.UncompressedSizeBytes > 0)
        {
            metrics.CompressionRatio = (double)metrics.UncompressedSizeBytes / metrics.CompressedSizeBytes;
        }
        
        // Determine compression type
        metrics.IsLossless = imageMetadata.Format switch
        {
            "PNG" => true,
            "BMP" => imageMetadata.Properties.TryGetValue("Compression", out var comp) && comp == "None",
            "TIFF" => true, // Can be lossy with JPEG compression, but typically lossless
            "GIF" => true,
            "JPEG" => false,
            "WebP" => imageMetadata.Properties.TryGetValue("Compression", out var webpComp) && webpComp == "Lossless",
            _ => false
        };
        
        // Estimate quality for lossy formats
        if (!metrics.IsLossless && imageMetadata.Format == "JPEG")
        {
            // Estimate JPEG quality from file size
            // Rule of thumb: Higher compression ratio = lower quality
            if (metrics.CompressionRatio > 30)
                metrics.EstimatedQuality = "Low (High Compression)";
            else if (metrics.CompressionRatio > 15)
                metrics.EstimatedQuality = "Medium";
            else if (metrics.CompressionRatio > 8)
                metrics.EstimatedQuality = "High";
            else
                metrics.EstimatedQuality = "Very High (Low Compression)";
        }
        else if (metrics.IsLossless)
        {
            metrics.EstimatedQuality = "Lossless (Perfect)";
        }
        
        return metrics;
    }

    public static CompressionMetrics AnalyzeVideo(VideoMetadata videoMetadata)
    {
        var metrics = new CompressionMetrics
        {
            MediaType = "Video",
            Format = videoMetadata.Container,
            CompressedSizeBytes = videoMetadata.FileSizeBytes
        };
        
        // Calculate uncompressed size (raw RGB frames)
        if (videoMetadata.Width > 0 && videoMetadata.Height > 0 && videoMetadata.DurationSeconds > 0)
        {
            var pixelCount = videoMetadata.Width * videoMetadata.Height;
            var bytesPerPixel = 3; // RGB
            var frameCount = (long)(videoMetadata.FrameRate * videoMetadata.DurationSeconds);
            
            metrics.UncompressedSizeBytes = pixelCount * bytesPerPixel * frameCount;
            
            // Calculate compression ratio
            if (metrics.UncompressedSizeBytes > 0)
            {
                metrics.CompressionRatio = (double)metrics.UncompressedSizeBytes / metrics.CompressedSizeBytes;
            }
        }
        
        // Determine compression type based on codec
        metrics.IsLossless = videoMetadata.VideoCodec switch
        {
            "Huffyuv" => true,
            "FFV1" => true,
            "Lagarith" => true,
            _ => false // H.264, H.265, VP8, VP9, AV1 are all lossy
        };
        
        // Estimate quality from bitrate
        if (videoMetadata.Width > 0 && videoMetadata.Height > 0 && videoMetadata.FrameRate > 0)
        {
            var bitrate = videoMetadata.EstimatedBitrate;
            var pixelsPerSecond = (long)videoMetadata.Width * videoMetadata.Height * videoMetadata.FrameRate;
            var bitsPerPixel = (double)bitrate / pixelsPerSecond * 1000; // Convert kbps to bps
            
            // Quality estimation based on bits per pixel
            if (metrics.IsLossless)
            {
                metrics.EstimatedQuality = "Lossless (Perfect)";
            }
            else if (bitsPerPixel < 0.05)
            {
                metrics.EstimatedQuality = "Low (Streaming)";
            }
            else if (bitsPerPixel < 0.15)
            {
                metrics.EstimatedQuality = "Medium (Web)";
            }
            else if (bitsPerPixel < 0.3)
            {
                metrics.EstimatedQuality = "High (HD)";
            }
            else
            {
                metrics.EstimatedQuality = "Very High (Broadcast)";
            }
            
            metrics.Properties["BitsPerPixel"] = bitsPerPixel.ToString("F4");
        }
        
        return metrics;
    }

    public static CompressionMetrics AnalyzeAudio(AudioMetadata audioMetadata)
    {
        var metrics = new CompressionMetrics
        {
            MediaType = "Audio",
            Format = audioMetadata.Format,
            CompressedSizeBytes = audioMetadata.FileSizeBytes
        };
        
        // Calculate uncompressed size (PCM)
        if (audioMetadata.SampleRate > 0 && audioMetadata.Channels > 0)
        {
            var bitDepth = audioMetadata.BitDepth > 0 ? audioMetadata.BitDepth : 16; // Default to 16-bit
            var bytesPerSample = bitDepth / 8;
            
            // Estimate duration from file size and bitrate
            if (audioMetadata.Bitrate > 0)
            {
                var durationSeconds = (double)audioMetadata.FileSizeBytes * 8 / (audioMetadata.Bitrate * 1000);
                var sampleCount = (long)(audioMetadata.SampleRate * durationSeconds);
                
                metrics.UncompressedSizeBytes = sampleCount * audioMetadata.Channels * bytesPerSample;
                
                // Calculate compression ratio
                if (metrics.UncompressedSizeBytes > 0)
                {
                    metrics.CompressionRatio = (double)metrics.UncompressedSizeBytes / metrics.CompressedSizeBytes;
                }
            }
        }
        
        // Determine compression type
        metrics.IsLossless = audioMetadata.Codec switch
        {
            "FLAC" => true,
            "PCM" => true,
            "ALAC" => true,
            "WavPack" => true,
            "Monkey's Audio" => true,
            _ => false // MP3, AAC, Vorbis, Opus are lossy
        };
        
        // Estimate quality from bitrate
        var bitrate = audioMetadata.Bitrate > 0 ? audioMetadata.Bitrate : audioMetadata.EstimatedBitrate;
        
        if (metrics.IsLossless)
        {
            metrics.EstimatedQuality = "Lossless (Perfect)";
        }
        else if (bitrate < 96)
        {
            metrics.EstimatedQuality = "Low (Voice/Streaming)";
        }
        else if (bitrate < 128)
        {
            metrics.EstimatedQuality = "Medium (Portable)";
        }
        else if (bitrate < 192)
        {
            metrics.EstimatedQuality = "High (Standard)";
        }
        else if (bitrate < 320)
        {
            metrics.EstimatedQuality = "Very High (Near CD)";
        }
        else
        {
            metrics.EstimatedQuality = "Maximum (Highest Lossy)";
        }
        
        metrics.Properties["BitrateKbps"] = bitrate.ToString();
        
        return metrics;
    }

    public static CompressionMetrics AnalyzeArchive(string archiveType, long compressedSize, long uncompressedSize)
    {
        var metrics = new CompressionMetrics
        {
            MediaType = "Archive",
            Format = archiveType,
            CompressedSizeBytes = (int)compressedSize,
            UncompressedSizeBytes = (int)uncompressedSize
        };
        
        if (uncompressedSize > 0)
        {
            metrics.CompressionRatio = (double)uncompressedSize / compressedSize;
        }
        
        // All archive formats are lossless
        metrics.IsLossless = true;
        metrics.EstimatedQuality = "Lossless (Perfect)";
        
        // Compare compression efficiency
        if (metrics.CompressionRatio > 4)
            metrics.Properties["Efficiency"] = "Excellent (4:1 or better)";
        else if (metrics.CompressionRatio > 2)
            metrics.Properties["Efficiency"] = "Good (2:1 or better)";
        else if (metrics.CompressionRatio > 1.2)
            metrics.Properties["Efficiency"] = "Moderate";
        else
            metrics.Properties["Efficiency"] = "Low (Not very compressible)";
        
        return metrics;
    }
}
