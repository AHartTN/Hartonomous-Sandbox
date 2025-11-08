namespace Hartonomous.Api.DTOs.Generation;

public class GenerateVideoRequest : GenerationRequestBase
{
    /// <summary>Target duration in milliseconds</summary>
    public int TargetDurationMs { get; init; } = 4000;

    /// <summary>Target frames per second</summary>
    public int TargetFps { get; init; } = 24;
}

/// <summary>
/// Response containing generated content
/// </summary>
