namespace Hartonomous.Core.Interfaces.Media;

/// <summary>
/// Comprehensive media toolkit orchestrator service.
/// Delegates to specialized services for extraction, analysis, and editing.
/// </summary>
public interface IMediaExtractionService
{
    /// <summary>
    /// Gets the frame extraction service.
    /// </summary>
    IFrameExtractionService FrameExtraction { get; }

    /// <summary>
    /// Gets the audio extraction service.
    /// </summary>
    IAudioExtractionService AudioExtraction { get; }

    /// <summary>
    /// Gets the audio analysis service.
    /// </summary>
    IAudioAnalysisService AudioAnalysis { get; }

    /// <summary>
    /// Gets the video editing service.
    /// </summary>
    IVideoEditingService VideoEditing { get; }

    /// <summary>
    /// Gets the audio effects service.
    /// </summary>
    IAudioEffectsService AudioEffects { get; }
}
