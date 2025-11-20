using System;

namespace Hartonomous.Infrastructure.Services.Vision;

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
