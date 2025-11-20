using System;
using System.Collections.Generic;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Base class for all media metadata.
/// Provides common fields across images, videos, audio.
/// </summary>
public abstract class MediaMetadata
{
    public string Format { get; set; } = "Unknown";
    public int FileSizeBytes { get; set; }
    public string? Codec { get; set; }
    
    // Common tags across all media types
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Year { get; set; }
    public string? Genre { get; set; }
    public string? Comment { get; set; }
    
    // Dimensions (applicable to image/video)
    public int Width { get; set; }
    public int Height { get; set; }
    
    // Temporal (applicable to video/audio)
    public double DurationSeconds { get; set; }
    
    // Format-specific properties
    public Dictionary<string, string> Properties { get; set; } = new();
    
    public abstract string MediaType { get; }
}

/// <summary>
/// Image-specific metadata (EXIF, dimensions, color space).
/// </summary>
public class ImageMetadata : MediaMetadata
{
    public override string MediaType => "Image";
    
    public int BitDepth { get; set; }
    public string ColorSpace { get; set; } = "Unknown";
    
    // EXIF data
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public int Orientation { get; set; }
    public DateTime? DateTaken { get; set; }
    public double? ExposureTime { get; set; }
    public double? FNumber { get; set; }
    public int? ISO { get; set; }
}

/// <summary>
/// Video-specific metadata (codec, frame rate, tracks).
/// </summary>
public class VideoMetadata : MediaMetadata
{
    public override string MediaType => "Video";
    
    public string Container { get; set; } = "Unknown";
    public double FrameRate { get; set; }
    
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int? VideoBitrate { get; set; }
    public int? AudioBitrate { get; set; }
    
    public bool HasVideo { get; set; }
    public bool HasAudio { get; set; }
    
    public int EstimatedBitrate => 
        DurationSeconds > 0 ? (int)((FileSizeBytes * 8) / DurationSeconds) : 0;
}

/// <summary>
/// Audio-specific metadata (sample rate, channels, ID3/Vorbis tags).
/// </summary>
public class AudioMetadata : MediaMetadata
{
    public override string MediaType => "Audio";
    
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public int BitDepth { get; set; }
    public int Bitrate { get; set; }
    
    public int EstimatedBitrate => 
        Bitrate > 0 ? Bitrate : (FileSizeBytes > 0 ? (FileSizeBytes * 8 / 1000) : 0);
}

/// <summary>
/// Interface for all metadata extractors.
/// Enables polymorphic extraction and format detection.
/// </summary>
public interface IMetadataExtractor
{
    /// <summary>
    /// Check if this extractor can handle the given data.
    /// </summary>
    bool CanHandle(byte[] data);
    
    /// <summary>
    /// Extract metadata from data.
    /// </summary>
    MediaMetadata ExtractMetadata(byte[] data);
}

/// <summary>
/// Service for atomizing metadata values into atoms with referential integrity.
/// Converts metadata (camera make, ISO, title, etc.) into character/numeric atoms
/// and links them to content atoms via AtomComposition.
/// </summary>
public interface IMetadataAtomizer
{
    /// <summary>
    /// Atomize metadata and create compositions linking metadata atoms to content atoms.
    /// </summary>
    /// <param name="metadata">Extracted metadata to atomize</param>
    /// <param name="contentAtomIds">IDs of content atoms (pixels, samples, etc.) to link to</param>
    /// <param name="source">Source identifier for provenance</param>
    /// <returns>List of metadata atom IDs created</returns>
    Task<List<Guid>> AtomizeMetadataAsync(
        MediaMetadata metadata, 
        List<Guid> contentAtomIds, 
        string source,
        CancellationToken cancellationToken = default);
}
