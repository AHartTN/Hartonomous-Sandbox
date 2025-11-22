namespace Hartonomous.Core.Models.Vision;

/// <summary>
/// Represents a region of text detected by OCR in an image.
/// </summary>
public class OcrRegion
{
    /// <summary>
    /// The extracted text content
    /// </summary>
    public required string Text { get; set; }
    
    /// <summary>
    /// The bounding box coordinates of the text region
    /// </summary>
    public required BoundingBox BoundingBox { get; set; }
    
    /// <summary>
    /// Confidence score for the OCR result (0.0 to 1.0)
    /// </summary>
    public required float Confidence { get; set; }
}
