using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class InferenceRequests : IInferenceRequests
{
    public long InferenceId { get; set; }

    public DateTime RequestTimestamp { get; set; }

    public DateTime? CompletionTimestamp { get; set; }

    public string? TaskType { get; set; }

    public string? InputData { get; set; }

    public byte[]? InputHash { get; set; }

    public string? CorrelationId { get; set; }

    public string? Status { get; set; }

    public double? Confidence { get; set; }

    public string? ModelsUsed { get; set; }

    public string? EnsembleStrategy { get; set; }

    public string? OutputData { get; set; }

    public string? OutputMetadata { get; set; }

    public int? TotalDurationMs { get; set; }

    public bool CacheHit { get; set; }

    public byte? UserRating { get; set; }

    public string? UserFeedback { get; set; }

    public int? Complexity { get; set; }

    public string? SlaTier { get; set; }

    public int? EstimatedResponseTimeMs { get; set; }

    public int? ModelId { get; set; }

    public virtual ICollection<InferenceSteps> InferenceSteps { get; set; } = new List<InferenceSteps>();

    public virtual Models? Model { get; set; }
}
