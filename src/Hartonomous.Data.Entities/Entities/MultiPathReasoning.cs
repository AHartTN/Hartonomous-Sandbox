using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class MultiPathReasoning : IMultiPathReasoning
{
    public int Id { get; set; }

    public Guid ProblemId { get; set; }

    public string BasePrompt { get; set; } = null!;

    public int NumPaths { get; set; }

    public int MaxDepth { get; set; }

    public int? BestPathId { get; set; }

    public string? ReasoningTree { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }
}
