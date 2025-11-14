using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAutonomousImprovementHistory
{
    Guid ImprovementId { get; set; }
    string AnalysisResults { get; set; }
    string GeneratedCode { get; set; }
    string TargetFile { get; set; }
    string ChangeType { get; set; }
    string RiskLevel { get; set; }
    string? EstimatedImpact { get; set; }
    string? GitCommitHash { get; set; }
    decimal? SuccessScore { get; set; }
    int? TestsPassed { get; set; }
    int? TestsFailed { get; set; }
    decimal? PerformanceDelta { get; set; }
    string? ErrorMessage { get; set; }
    bool WasDeployed { get; set; }
    bool WasRolledBack { get; set; }
    DateTime StartedAt { get; set; }
    DateTime? CompletedAt { get; set; }
    DateTime? RolledBackAt { get; set; }
}
