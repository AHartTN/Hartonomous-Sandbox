using Hartonomous.Core.Enums;

namespace Hartonomous.Core.Interfaces.Media;

/// <summary>
/// Service for extracting frames from video files.
/// </summary>
public interface IFrameExtractionService
{
    /// <summary>
    /// Extracts frames from a video file with filtering options.
    /// </summary>
    /// <param name="videoPath">Path to input video file.</param>
    /// <param name="outputDirectory">Directory to save extracted frames.</param>
    /// <param name="options">Frame extraction options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of extracted frame paths with timestamps.</returns>
    Task<List<(TimeSpan Timestamp, string FilePath)>> ExtractFramesAsync(
        string videoPath,
        string outputDirectory,
        FrameExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates thumbnail contact sheet from video (grid of frames).
    /// </summary>
    Task<bool> GenerateContactSheetAsync(
        string inputPath,
        string outputPath,
        int columns = 4,
        int rows = 4,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for frame extraction from video files.
/// </summary>
public class FrameExtractionOptions
{
    /// <summary>
    /// How many frames per second to extract (e.g., 1.0 = 1 frame every second, 0.5 = 1 frame every 2 seconds).
    /// </summary>
    public double FramesPerSecond { get; set; } = 1.0;

    /// <summary>
    /// Start time for extraction (null = from beginning).
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// End time for extraction (null = until end).
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Maximum number of frames to extract (null = no limit).
    /// </summary>
    public int? MaxFrames { get; set; }

    /// <summary>
    /// Output format for frames.
    /// </summary>
    public MediaFormat OutputFormat { get; set; } = MediaFormat.PNG;

    /// <summary>
    /// Optional resize dimensions (null = keep original size).
    /// </summary>
    public (int Width, int Height)? Resize { get; set; }

    /// <summary>
    /// JPEG quality (1-100) if OutputFormat is JPEG.
    /// </summary>
    public int JpegQuality { get; set; } = 85;
}
