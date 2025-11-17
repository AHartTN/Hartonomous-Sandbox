using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class SelfConsistencyResult : ISelfConsistencyResult
{
    public int Id { get; set; }

    public Guid ProblemId { get; set; }

    public string Prompt { get; set; } = null!;

    public int NumSamples { get; set; }

    public string? ConsensusAnswer { get; set; }

    public double AgreementRatio { get; set; }

    public string? ConsensusMetrics { get; set; }

    public string? SampleData { get; set; }

    public int DurationMs { get; set; }

    public DateTime CreatedAt { get; set; }
}
