using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hartonomous.Infrastructure.Services.Vision;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Serializes media metadata to JSON for storage in Atom.Metadata field (native json data type).
/// Provides consistent JSON formatting across all media types.
/// </summary>
public static class MetadataJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false, // Compact JSON for database storage
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Serialize ImageMetadata to JSON string for Atom.Metadata field.
    /// </summary>
    public static string SerializeImageMetadata(ImageMetadata metadata, CompressionMetrics? compression = null)
    {
        var json = new
        {
            mediaType = "image",
            format = metadata.Format,
            width = metadata.Width,
            height = metadata.Height,
            bitDepth = metadata.BitDepth,
            colorSpace = metadata.ColorSpace,
            fileSizeBytes = metadata.FileSizeBytes,
            
            // EXIF data (if available)
            exif = metadata.CameraMake != null || metadata.CameraModel != null || metadata.ISO.HasValue 
                ? new
                {
                    cameraMake = metadata.CameraMake,
                    cameraModel = metadata.CameraModel,
                    orientation = metadata.Orientation,
                    dateTaken = metadata.DateTaken?.ToString("O"), // ISO 8601 format
                    exposureTime = metadata.ExposureTime,
                    fNumber = metadata.FNumber,
                    iso = metadata.ISO
                }
                : null,
            
            // Compression metrics (if analyzed)
            compression = compression != null
                ? new
                {
                    compressedSizeBytes = compression.CompressedSizeBytes,
                    uncompressedSizeBytes = compression.UncompressedSizeBytes,
                    compressionRatio = compression.CompressionRatio,
                    isLossless = compression.IsLossless,
                    estimatedQuality = compression.EstimatedQuality,
                    compressionType = compression.CompressionType
                }
                : null,
            
            // Additional properties
            properties = metadata.Properties?.Count > 0 ? metadata.Properties : null
        };

        return JsonSerializer.Serialize(json, Options);
    }

    /// <summary>
    /// Serialize VideoMetadata to JSON string for Atom.Metadata field.
    /// </summary>
    public static string SerializeVideoMetadata(VideoMetadata metadata, CompressionMetrics? compression = null)
    {
        var json = new
        {
            mediaType = "video",
            format = metadata.Format,
            container = metadata.Container,
            width = metadata.Width,
            height = metadata.Height,
            durationSeconds = metadata.DurationSeconds,
            frameRate = metadata.FrameRate,
            fileSizeBytes = metadata.FileSizeBytes,
            
            // Video codec info
            video = metadata.HasVideo
                ? new
                {
                    codec = metadata.VideoCodec,
                    bitrate = metadata.VideoBitrate,
                    estimatedBitrate = metadata.EstimatedBitrate
                }
                : null,
            
            // Audio codec info
            audio = metadata.HasAudio
                ? new
                {
                    codec = metadata.AudioCodec,
                    bitrate = metadata.AudioBitrate
                }
                : null,
            
            // Compression metrics
            compression = compression != null
                ? new
                {
                    compressedSizeBytes = compression.CompressedSizeBytes,
                    uncompressedSizeBytes = compression.UncompressedSizeBytes,
                    compressionRatio = compression.CompressionRatio,
                    isLossless = compression.IsLossless,
                    estimatedQuality = compression.EstimatedQuality,
                    compressionType = compression.CompressionType
                }
                : null,
            
            // Metadata tags
            title = metadata.Title,
            artist = metadata.Artist,
            album = metadata.Album,
            year = metadata.Year,
            genre = metadata.Genre,
            comment = metadata.Comment,
            
            // Additional properties
            properties = metadata.Properties?.Count > 0 ? metadata.Properties : null
        };

        return JsonSerializer.Serialize(json, Options);
    }

    /// <summary>
    /// Serialize AudioMetadata to JSON string for Atom.Metadata field.
    /// </summary>
    public static string SerializeAudioMetadata(AudioMetadata metadata, CompressionMetrics? compression = null)
    {
        var json = new
        {
            mediaType = "audio",
            format = metadata.Format,
            codec = metadata.Codec,
            durationSeconds = metadata.DurationSeconds,
            sampleRate = metadata.SampleRate,
            channels = metadata.Channels,
            bitDepth = metadata.BitDepth,
            bitrate = metadata.Bitrate,
            estimatedBitrate = metadata.EstimatedBitrate,
            fileSizeBytes = metadata.FileSizeBytes,
            
            // Compression metrics
            compression = compression != null
                ? new
                {
                    compressedSizeBytes = compression.CompressedSizeBytes,
                    uncompressedSizeBytes = compression.UncompressedSizeBytes,
                    compressionRatio = compression.CompressionRatio,
                    isLossless = compression.IsLossless,
                    estimatedQuality = compression.EstimatedQuality,
                    compressionType = compression.CompressionType
                }
                : null,
            
            // ID3/Vorbis tags
            title = metadata.Title,
            artist = metadata.Artist,
            album = metadata.Album,
            year = metadata.Year,
            genre = metadata.Genre,
            comment = metadata.Comment,
            
            // Additional properties
            properties = metadata.Properties?.Count > 0 ? metadata.Properties : null
        };

        return JsonSerializer.Serialize(json, Options);
    }
}
