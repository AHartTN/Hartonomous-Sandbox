namespace Hartonomous.Core.Enums;

/// <summary>
/// Type of streaming data being ingested.
/// </summary>
public enum StreamType
{
    /// <summary>
    /// Telemetry sensor data.
    /// </summary>
    Telemetry,

    /// <summary>
    /// Video stream data.
    /// </summary>
    Video,

    /// <summary>
    /// Audio stream data.
    /// </summary>
    Audio,

    /// <summary>
    /// Mixed media stream (combination of types).
    /// </summary>
    Mixed
}
