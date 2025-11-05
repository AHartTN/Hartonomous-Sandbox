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

public class SubmitFeedbackResponse
{
    public long FeedbackId { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
}

public class UpdateImportanceRequest
{
    [Required]
    public required List<AtomImportanceUpdate> Updates { get; set; }
    
    public string? Reason { get; set; }
}

public class AtomImportanceUpdate
{
    [Required]
    public long AtomId { get; set; }
    
    [Range(-1.0, 1.0)]
    public double ImportanceDelta { get; set; }
}

public class UpdateImportanceResponse
{
    public int UpdatedCount { get; set; }
    public required List<ImportanceUpdateResult> Results { get; set; }
}

public class ImportanceUpdateResult
{
    public long AtomId { get; set; }
    public bool Success { get; set; }
    public double? PreviousImportance { get; set; }
    public double? NewImportance { get; set; }
    public string? Message { get; set; }
}

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

public class TriggerFineTuningResponse
{
    public long FineTuningJobId { get; set; }
    public required string Status { get; set; }
    public int FeedbackSamplesUsed { get; set; }
    public DateTime StartedAt { get; set; }
    public string? Message { get; set; }
}

public class GetFeedbackSummaryRequest
{
    public int? ModelId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GetFeedbackSummaryResponse
{
    public long TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public required Dictionary<int, long> RatingDistribution { get; set; }
    public long PositiveFeedbackCount { get; set; }
    public long NegativeFeedbackCount { get; set; }
    public required List<FeedbackTrendPoint> Trends { get; set; }
}

public class FeedbackTrendPoint
{
    public DateTime Date { get; set; }
    public long FeedbackCount { get; set; }
    public double AverageRating { get; set; }
}

public class RetrainModelRequest
{
    [Required]
    public int ModelId { get; set; }
    
    public required List<long> AtomIds { get; set; }
    
    public string Strategy { get; set; } = "importance_boost"; // importance_boost, fine_tune, distill
    
    [Range(0.0, 2.0)]
    public double BoostFactor { get; set; } = 1.5;
}

public class RetrainModelResponse
{
    public int ModelId { get; set; }
    public required string Strategy { get; set; }
    public int AtomsAffected { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TimeSpan Duration { get; set; }
}
