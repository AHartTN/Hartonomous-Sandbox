namespace Hartonomous.Api.DTOs.Analytics;

public class ModelPerformanceRequest
{
    public int? ModelId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
