using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IReasoningChain
{
    int Id { get; set; }
    Guid ProblemId { get; set; }
    string ReasoningType { get; set; }
    string? ChainData { get; set; }
    string? CoherenceMetrics { get; set; }
    int TotalSteps { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
}
