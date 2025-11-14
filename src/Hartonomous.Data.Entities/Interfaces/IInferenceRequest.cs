using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IInferenceRequest
{
    long InferenceId { get; set; }
    DateTime RequestTimestamp { get; set; }
    DateTime? CompletionTimestamp { get; set; }
    string? TaskType { get; set; }
    string? InputData { get; set; }
    byte[]? InputHash { get; set; }
    string? CorrelationId { get; set; }
    string? Status { get; set; }
    double? Confidence { get; set; }
    string? ModelsUsed { get; set; }
    string? EnsembleStrategy { get; set; }
    string? OutputData { get; set; }
    string? OutputMetadata { get; set; }
    int? TotalDurationMs { get; set; }
    bool CacheHit { get; set; }
    byte? UserRating { get; set; }
    string? UserFeedback { get; set; }
    int? Complexity { get; set; }
    string? SlaTier { get; set; }
    int? EstimatedResponseTimeMs { get; set; }
    int? ModelId { get; set; }
    ICollection<InferenceStep> InferenceSteps { get; set; }
    Model? Model { get; set; }
}
