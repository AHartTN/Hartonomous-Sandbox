namespace Hartonomous.Core.Models.Vision;

/// <summary>
/// Represents a rectangular bounding box with pixel coordinates.
/// </summary>
public class BoundingBox
{
    /// <summary>
    /// X coordinate of the top-left corner
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// Y coordinate of the top-left corner
    /// </summary>
    public int Y { get; set; }
    
    /// <summary>
    /// Width of the bounding box in pixels
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Height of the bounding box in pixels
    /// </summary>
    public int Height { get; set; }
}
