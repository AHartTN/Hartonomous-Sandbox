namespace Hartonomous.Core.Interfaces.Media;

/// <summary>
/// Service for video editing operations (clipping, speed adjustment, effects).
/// </summary>
public interface IVideoEditingService
{
    /// <summary>
    /// Extracts a specific time segment from a video file.
    /// </summary>
    Task<bool> ExtractVideoSegmentAsync(
        string inputPath,
        string outputPath,
        TimeSpan startTime,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a GIF from a video segment.
    /// </summary>
    Task<bool> CreateGifAsync(
        string inputPath,
        string outputPath,
        TimeSpan? startTime = null,
        TimeSpan? duration = null,
        int fps = 10,
        int? width = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Concatenates multiple video files into a single output.
    /// </summary>
    Task<bool> ConcatenateVideosAsync(
        string[] inputPaths,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a video collage/grid from multiple input videos.
    /// </summary>
    Task<bool> CreateVideoGridAsync(
        string[] inputPaths,
        string outputPath,
        int columns = 2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds text overlay to video.
    /// </summary>
    Task<bool> AddTextOverlayAsync(
        string inputPath,
        string outputPath,
        string text,
        string position = "center",
        string fontColor = "white",
        int fontSize = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes video playback speed.
    /// </summary>
    Task<bool> ChangeVideoSpeedAsync(
        string inputPath,
        string outputPath,
        double speedFactor = 1.0,
        CancellationToken cancellationToken = default);
}
