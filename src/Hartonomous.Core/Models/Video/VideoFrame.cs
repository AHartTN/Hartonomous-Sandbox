namespace Hartonomous.Core.Models.Video;

/// <summary>
/// Single video frame from a stream.
/// </summary>
public class VideoFrame
{
    public required string StreamId { get; set; }
    public required string FrameId { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Raw RGBA pixel data (4 bytes per pixel: R, G, B, A).
    /// Length must be Width * Height * 4.
    /// </summary>
    public required byte[] PixelData { get; set; }
    
    /// <summary>
    /// Frame rate for reference (frames per second).
    /// </summary>
    public double FrameRate { get; set; }
}
