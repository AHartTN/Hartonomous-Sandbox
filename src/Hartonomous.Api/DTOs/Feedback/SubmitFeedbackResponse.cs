namespace Hartonomous.Api.DTOs.Feedback;

public class SubmitFeedbackResponse
{
    public long FeedbackId { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
}
