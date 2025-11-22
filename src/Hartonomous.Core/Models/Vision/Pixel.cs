namespace Hartonomous.Core.Models.Vision;

/// <summary>
/// Represents a single pixel with RGBA color components.
/// </summary>
public struct Pixel
{
    /// <summary>
    /// Red component (0-255)
    /// </summary>
    public byte R { get; set; }
    
    /// <summary>
    /// Green component (0-255)
    /// </summary>
    public byte G { get; set; }
    
    /// <summary>
    /// Blue component (0-255)
    /// </summary>
    public byte B { get; set; }
    
    /// <summary>
    /// Alpha/transparency component (0-255, where 255 is fully opaque)
    /// </summary>
    public byte A { get; set; }
}
