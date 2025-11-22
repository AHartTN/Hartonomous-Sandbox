namespace Hartonomous.Core.Models.Vision;

/// <summary>
/// Represents a dominant color in an image with its RGB values and coverage percentage.
/// </summary>
public class DominantColor
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
    /// Percentage of the image covered by this color (0.0 to 100.0)
    /// </summary>
    public float Percentage { get; set; }
}
