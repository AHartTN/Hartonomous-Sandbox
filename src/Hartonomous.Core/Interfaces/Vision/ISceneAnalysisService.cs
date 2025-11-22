using Hartonomous.Core.Models.Vision;

namespace Hartonomous.Core.Interfaces.Vision;

/// <summary>
/// Service for analyzing image scenes - extracting captions, tags, and dominant colors.
/// </summary>
public interface ISceneAnalysisService
{
    /// <summary>
    /// Analyzes an image to extract semantic information.
    /// </summary>
    /// <param name="imageData">The image data in bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scene information including caption, tags, and dominant colors</returns>
    Task<SceneInfo> AnalyzeSceneAsync(byte[] imageData, CancellationToken cancellationToken);
}
