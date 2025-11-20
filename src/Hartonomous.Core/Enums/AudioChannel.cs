namespace Hartonomous.Core.Enums;

/// <summary>
/// Audio channel configuration.
/// </summary>
public enum AudioChannel
{
    /// <summary>
    /// Single channel (mono).
    /// </summary>
    Mono = 1,

    /// <summary>
    /// Two channels (stereo).
    /// </summary>
    Stereo = 2,

    /// <summary>
    /// 2.1 surround (left, right, subwoofer).
    /// </summary>
    Surround21 = 3,

    /// <summary>
    /// 5.1 surround (front left/right, center, rear left/right, subwoofer).
    /// </summary>
    Surround51 = 6,

    /// <summary>
    /// 7.1 surround (front left/right, center, side left/right, rear left/right, subwoofer).
    /// </summary>
    Surround71 = 8
}
