namespace Hartonomous.Core.Models.Vision;

/// <summary>
/// Represents an object detected in an image.
/// </summary>
public class DetectedObject
{
    /// <summary>
    /// The label/class of the detected object (e.g., "person", "car", "dog")
    /// </summary>
    public required string Label { get; set; }
    
    /// <summary>
    /// The bounding box coordinates of the detected object
    /// </summary>
    public required BoundingBox BoundingBox { get; set; }
    
    /// <summary>
    /// Confidence score for the detection (0.0 to 1.0)
    /// </summary>
    public required float Confidence { get; set; }
}
