namespace Hartonomous.Core.Models.Vision;

/// <summary>
/// Represents a semantic tag detected in an image.
/// </summary>
public class Tag
{
    /// <summary>
    /// The tag name (e.g., "outdoor", "sky", "building")
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Confidence score for the tag (0.0 to 1.0)
    /// </summary>
    public required float Confidence { get; set; }
}
