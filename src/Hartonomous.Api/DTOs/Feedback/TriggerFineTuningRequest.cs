using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Feedback;

public class TriggerFineTuningRequest
{
    [Required]
    public int ModelId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [Range(1, 10000)]
    public int? FeedbackLimit { get; set; }

    [Range(0.0, 1.0)]
    public double LearningRate { get; set; } = 0.001;

    public int Epochs { get; set; } = 1;
}
