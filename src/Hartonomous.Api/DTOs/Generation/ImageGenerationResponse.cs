namespace Hartonomous.Api.DTOs.Generation;

public sealed class ImageGenerationResponse
{
    public long InferenceId { get; set; }
    public required string Prompt { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Steps { get; set; }
    public int DurationMs { get; set; }
    public string? CandidateImages { get; set; }
    public List<ImagePatch>? Patches { get; set; }
}

public sealed class ImagePatch
{
    public int PatchX { get; set; }
    public int PatchY { get; set; }
    public double SpatialX { get; set; }
    public double SpatialY { get; set; }
    public double SpatialZ { get; set; }
}
