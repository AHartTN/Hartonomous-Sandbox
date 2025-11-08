namespace Hartonomous.Api.DTOs.Models;

public class DistillationResult
{
    public int StudentModelId { get; set; }
    public required string StudentName { get; set; }
    public int ParentModelId { get; set; }
    public long OriginalTensorAtoms { get; set; }
    public long StudentTensorAtoms { get; set; }
    public double CompressionRatio { get; set; }
    public double RetentionPercent { get; set; }
}
