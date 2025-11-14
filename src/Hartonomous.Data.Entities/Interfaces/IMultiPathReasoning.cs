using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IMultiPathReasoning
{
    int Id { get; set; }
    Guid ProblemId { get; set; }
    string BasePrompt { get; set; }
    int NumPaths { get; set; }
    int MaxDepth { get; set; }
    int? BestPathId { get; set; }
    string? ReasoningTree { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
}
