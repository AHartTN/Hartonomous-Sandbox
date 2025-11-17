using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AttentionInferenceResults : IAttentionInferenceResults
{
    public int Id { get; set; }

    public Guid ProblemId { get; set; }

    public string Query { get; set; } = null!;

    public int ModelId { get; set; }

    public int MaxReasoningSteps { get; set; }

    public int AttentionHeads { get; set; }

    public string? ReasoningSteps { get; set; }

    public int TotalSteps { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Models Model { get; set; } = null!;
}
