using Hartonomous.Core.Models.Vision;

namespace Hartonomous.Core.Interfaces.Vision;

/// <summary>
/// Service for extracting text from images using Optical Character Recognition (OCR).
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extracts text regions from an image.
    /// </summary>
    /// <param name="imageData">The image data in bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of detected text regions with bounding boxes and confidence scores</returns>
    Task<List<OcrRegion>> ExtractTextAsync(byte[] imageData, CancellationToken cancellationToken);
}
