using Hartonomous.Core.Models.Vision;

namespace Hartonomous.Core.Interfaces.Vision;

/// <summary>
/// Service for detecting objects in images.
/// </summary>
public interface IObjectDetectionService
{
    /// <summary>
    /// Detects objects in an image.
    /// </summary>
    /// <param name="imageData">The image data in bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of detected objects with labels, bounding boxes, and confidence scores</returns>
    Task<List<DetectedObject>> DetectObjectsAsync(byte[] imageData, CancellationToken cancellationToken);
}
