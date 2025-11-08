namespace Hartonomous.Api.DTOs.Generation;

public class GenerateImageRequest : GenerationRequestBase
{
    /// <summary>Image width in pixels</summary>
    public int Width { get; init; } = 512;

    /// <summary>Image height in pixels</summary>
    public int Height { get; init; } = 512;

    /// <summary>Patch size for spatial diffusion</summary>
    public int PatchSize { get; init; } = 32;

    /// <summary>Number of diffusion steps</summary>
    public int Steps { get; init; } = 32;

    /// <summary>Guidance scale for prompt adherence</summary>
    public double GuidanceScale { get; init; } = 6.5;

    /// <summary>Output format: 'patches', 'binary', 'geometry'</summary>
    public string OutputFormat { get; init; } = "patches";
}

/// <summary>
/// Request to generate audio from a text prompt
/// </summary>
