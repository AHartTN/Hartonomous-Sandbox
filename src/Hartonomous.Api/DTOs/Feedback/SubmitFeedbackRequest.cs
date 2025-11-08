using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Feedback;

public class SubmitFeedbackRequest
{
    [Required]
    public long InferenceId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    public string? FeedbackText { get; set; }

    public List<long>? CorrectAtomIds { get; set; }
    public List<long>? IncorrectAtomIds { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
}
