namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Feature flags for controlling image processing pipeline.
/// Each flag enables/disables a specific processing stage.
/// </summary>
public class ImageProcessingOptions
{
    /// <summary>
    /// Extract text from image using OCR.
    /// Default: false (pixels only).
    /// </summary>
    public bool ExtractText { get; set; } = false;

    /// <summary>
    /// Detect objects in image (bounding boxes and shapes).
    /// Default: false (pixels only).
    /// </summary>
    public bool DetectObjects { get; set; } = false;

    /// <summary>
    /// Identify/classify detected objects (requires DetectObjects = true).
    /// When false, objects are detected but not classified.
    /// Default: false.
    /// </summary>
    public bool IdentifyObjects { get; set; } = false;

    /// <summary>
    /// Analyze scene for colors, tags, and caption generation.
    /// Default: false (pixels only).
    /// </summary>
    public bool AnalyzeScene { get; set; } = false;

    /// <summary>
    /// Preset: Enable all features.
    /// </summary>
    public static ImageProcessingOptions All => new()
    {
        ExtractText = true,
        DetectObjects = true,
        IdentifyObjects = true,
        AnalyzeScene = true
    };

    /// <summary>
    /// Preset: Only OCR (document scanning).
    /// </summary>
    public static ImageProcessingOptions DocumentScan => new()
    {
        ExtractText = true,
        DetectObjects = false,
        IdentifyObjects = false,
        AnalyzeScene = false
    };

    /// <summary>
    /// Preset: Only object detection (no identification).
    /// </summary>
    public static ImageProcessingOptions ObjectDetectionOnly => new()
    {
        ExtractText = false,
        DetectObjects = true,
        IdentifyObjects = false,
        AnalyzeScene = false
    };

    /// <summary>
    /// Preset: Full object analysis (detection + identification).
    /// </summary>
    public static ImageProcessingOptions ObjectAnalysis => new()
    {
        ExtractText = false,
        DetectObjects = true,
        IdentifyObjects = true,
        AnalyzeScene = false
    };

    /// <summary>
    /// Preset: Scene understanding (colors, tags, caption).
    /// </summary>
    public static ImageProcessingOptions SceneUnderstanding => new()
    {
        ExtractText = false,
        DetectObjects = false,
        IdentifyObjects = false,
        AnalyzeScene = true
    };

    /// <summary>
    /// Preset: Pixels only (fastest, no semantic analysis).
    /// </summary>
    public static ImageProcessingOptions PixelsOnly => new()
    {
        ExtractText = false,
        DetectObjects = false,
        IdentifyObjects = false,
        AnalyzeScene = false
    };
}
