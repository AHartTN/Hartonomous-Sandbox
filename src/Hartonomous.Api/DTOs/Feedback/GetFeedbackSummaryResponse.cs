namespace Hartonomous.Api.DTOs.Feedback;

public class GetFeedbackSummaryResponse
{
    public long TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public required Dictionary<int, long> RatingDistribution { get; set; }
    public long PositiveFeedbackCount { get; set; }
    public long NegativeFeedbackCount { get; set; }
    public required List<FeedbackTrendPoint> Trends { get; set; }
}
