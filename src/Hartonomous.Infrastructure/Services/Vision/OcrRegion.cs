namespace Hartonomous.Infrastructure.Services.Vision;

public class OcrRegion
{
    public required string Text { get; set; }
    public required BoundingBox BoundingBox { get; set; }
    public required float Confidence { get; set; }
}
