namespace Hartonomous.Api.DTOs.Feedback;

public class TriggerFineTuningResponse
{
    public long FineTuningJobId { get; set; }
    public required string Status { get; set; }
    public int FeedbackSamplesUsed { get; set; }
    public DateTime StartedAt { get; set; }
    public string? Message { get; set; }
}
