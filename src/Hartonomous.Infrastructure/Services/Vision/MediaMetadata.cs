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
