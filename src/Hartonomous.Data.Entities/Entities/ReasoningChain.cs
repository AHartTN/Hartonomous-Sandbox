using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class ReasoningChain : IReasoningChain
{
    public int Id { get; set; }

    public Guid ProblemId { get; set; }

    public string ReasoningType { get; set; } = null!;

    public string? ChainData { get; set; }

    public string? CoherenceMetrics { get; set; }

    public int TotalSteps { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }
}
