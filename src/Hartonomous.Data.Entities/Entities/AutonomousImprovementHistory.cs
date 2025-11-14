using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AutonomousImprovementHistory : IAutonomousImprovementHistory
{
    public Guid ImprovementId { get; set; }

    public string AnalysisResults { get; set; } = null!;

    public string GeneratedCode { get; set; } = null!;

    public string TargetFile { get; set; } = null!;

    public string ChangeType { get; set; } = null!;

    public string RiskLevel { get; set; } = null!;

    public string? EstimatedImpact { get; set; }

    public string? GitCommitHash { get; set; }

    public decimal? SuccessScore { get; set; }

    public int? TestsPassed { get; set; }

    public int? TestsFailed { get; set; }

    public decimal? PerformanceDelta { get; set; }

    public string? ErrorMessage { get; set; }

    public bool WasDeployed { get; set; }

    public bool WasRolledBack { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? RolledBackAt { get; set; }
}
