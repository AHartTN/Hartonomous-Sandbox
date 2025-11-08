namespace Hartonomous.Api.DTOs.Models;

public class DistillationRequest
{
    public required string StudentName { get; set; }
    public List<int>? LayerIndices { get; set; }
    public double ImportanceThreshold { get; set; } = 0.5;
}
