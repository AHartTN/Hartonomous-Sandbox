namespace Hartonomous.Core.Interfaces.Media;

/// <summary>
/// Service for applying audio effects (normalization, fades, silence removal).
/// </summary>
public interface IAudioEffectsService
{
    /// <summary>
    /// Normalizes audio volume to target level.
    /// </summary>
    Task<bool> NormalizeAudioAsync(
        string inputPath,
        string outputPath,
        double targetLoudnessDb = -16.0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies fade in/out effect to audio.
    /// </summary>
    Task<bool> FadeAudioAsync(
        string inputPath,
        string outputPath,
        TimeSpan? fadeInDuration = null,
        TimeSpan? fadeOutDuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes silence from audio.
    /// </summary>
    Task<bool> RemoveSilenceAsync(
        string inputPath,
        string outputPath,
        double silenceThresholdDb = -40.0,
        double minSilenceDuration = 0.5,
        CancellationToken cancellationToken = default);
}
