namespace Hartonomous.Api.DTOs.Models;

public class LayerSummary
{
    public long LayerId { get; set; }
    public int LayerIdx { get; set; }
    public string? LayerName { get; set; }
    public string? LayerType { get; set; }
    public long? ParameterCount { get; set; }
}
