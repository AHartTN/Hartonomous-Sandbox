namespace Hartonomous.Core.Models.Vision;

/// <summary>
/// Comprehensive scene analysis information extracted from an image.
/// </summary>
public class SceneInfo
{
    /// <summary>
    /// Generated caption describing the image content
    /// </summary>
    public string? Caption { get; set; }
    
    /// <summary>
    /// Confidence score for the generated caption (0.0 to 1.0)
    /// </summary>
    public float CaptionConfidence { get; set; }
    
    /// <summary>
    /// Semantic tags identified in the image
    /// </summary>
    public List<Tag> Tags { get; set; } = new();
    
    /// <summary>
    /// Dominant colors extracted from the image
    /// </summary>
    public List<DominantColor> DominantColors { get; set; } = new();
}
