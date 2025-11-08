namespace Hartonomous.Api.DTOs.Operations;

public class IndexOperationResult
{
    public required string IndexName { get; set; }
    public required string TableName { get; set; }
    public required string Operation { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TimeSpan Duration { get; set; }
    public double? FragmentationBefore { get; set; }
    public double? FragmentationAfter { get; set; }
}
