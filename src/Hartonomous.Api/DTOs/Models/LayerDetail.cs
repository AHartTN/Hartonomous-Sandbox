namespace Hartonomous.Api.DTOs.Models;

public class LayerDetail
{
    public long LayerId { get; set; }
    public int LayerIdx { get; set; }
    public string? LayerName { get; set; }
    public string? LayerType { get; set; }
    public long? ParameterCount { get; set; }
    public string? TensorShape { get; set; }
    public string? TensorDtype { get; set; }
    public double? CacheHitRate { get; set; }
    public double? AvgComputeTimeMs { get; set; }
    public int TensorAtomCount { get; set; }
    public double? AvgImportanceScore { get; set; }
}
