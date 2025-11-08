namespace Hartonomous.Api.DTOs.Feedback;

public class GetFeedbackSummaryRequest
{
    public int? ModelId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
