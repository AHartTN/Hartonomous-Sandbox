namespace Hartonomous.Api.DTOs.Spatial;

public sealed class FusionWeights
{
    public double VectorWeight { get; set; }
    public double KeywordWeight { get; set; }
    public double SpatialWeight { get; set; }
}
