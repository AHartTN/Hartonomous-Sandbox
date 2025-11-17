using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface ISelfConsistencyResult
{
    int Id { get; set; }
    Guid ProblemId { get; set; }
    string Prompt { get; set; }
    int NumSamples { get; set; }
    string? ConsensusAnswer { get; set; }
    double AgreementRatio { get; set; }
    string? ConsensusMetrics { get; set; }
    string? SampleData { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
}
