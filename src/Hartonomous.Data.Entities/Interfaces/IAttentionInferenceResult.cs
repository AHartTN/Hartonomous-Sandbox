using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAttentionInferenceResult
{
    int Id { get; set; }
    Guid ProblemId { get; set; }
    string Query { get; set; }
    int ModelId { get; set; }
    int MaxReasoningSteps { get; set; }
    int AttentionHeads { get; set; }
    string? ReasoningSteps { get; set; }
    int TotalSteps { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
    Model Model { get; set; }
}
