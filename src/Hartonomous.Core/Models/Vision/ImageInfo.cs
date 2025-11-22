namespace Hartonomous.Core.Models.Vision;

/// <summary>
/// Decoded image information including dimensions and pixel accessor.
/// </summary>
public class ImageInfo
{
    /// <summary>
    /// Image width in pixels
    /// </summary>
    public required int Width { get; set; }
    
    /// <summary>
    /// Image height in pixels
    /// </summary>
    public required int Height { get; set; }
    
    /// <summary>
    /// Image format (e.g., "PNG", "JPEG", "GIF")
    /// </summary>
    public required string Format { get; set; }
    
    /// <summary>
    /// Function to retrieve a pixel at the specified coordinates
    /// </summary>
    public required Func<int, int, Pixel> GetPixel { get; set; }
}
