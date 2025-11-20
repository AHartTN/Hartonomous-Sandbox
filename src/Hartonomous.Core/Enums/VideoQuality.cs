namespace Hartonomous.Core.Enums;

/// <summary>
/// Video quality presets for encoding.
/// </summary>
public enum VideoQuality
{
    /// <summary>
    /// Low quality (high compression, smaller file size).
    /// </summary>
    Low,

    /// <summary>
    /// Medium quality (balanced compression and quality).
    /// </summary>
    Medium,

    /// <summary>
    /// High quality (low compression, larger file size).
    /// </summary>
    High,

    /// <summary>
    /// Very high quality (minimal compression).
    /// </summary>
    VeryHigh,

    /// <summary>
    /// Lossless quality (no compression).
    /// </summary>
    Lossless
}
