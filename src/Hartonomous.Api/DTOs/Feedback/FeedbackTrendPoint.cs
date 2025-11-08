namespace Hartonomous.Api.DTOs.Feedback;

public class FeedbackTrendPoint
{
    public DateTime Date { get; set; }
    public long FeedbackCount { get; set; }
    public double AverageRating { get; set; }
}
