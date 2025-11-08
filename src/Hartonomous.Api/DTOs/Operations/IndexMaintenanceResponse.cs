namespace Hartonomous.Api.DTOs.Operations;

public class IndexMaintenanceResponse
{
    public required List<IndexOperationResult> Results { get; set; }
    public TimeSpan TotalDuration { get; set; }
}
