namespace Hartonomous.Api.DTOs.Generation;

public sealed class ImageGenerationRequest
{
    public required string Prompt { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? PatchSize { get; set; }
    public int? Steps { get; set; }
    public double? GuidanceScale { get; set; }
    public int? TopK { get; set; }
    public string? OutputFormat { get; set; }
    public string? ModelIds { get; set; }
}
