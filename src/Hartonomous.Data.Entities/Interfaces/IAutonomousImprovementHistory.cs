using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IAutonomousImprovementHistory
{
    Guid ImprovementId { get; set; }
    string? ImprovementType { get; set; }
    string? TargetEntity { get; set; }
    long? TargetId { get; set; }
    string? AnalysisResults { get; set; }
    string? GeneratedCode { get; set; }
    string? TargetFile { get; set; }
    string? ChangeType { get; set; }
    string? OldValue { get; set; }
    string? NewValue { get; set; }
    string? RiskLevel { get; set; }
    string? EstimatedImpact { get; set; }
    string? GitCommitHash { get; set; }
    string? ApprovedBy { get; set; }
    DateTime? ExecutedAt { get; set; }
    bool? Success { get; set; }
    string? Notes { get; set; }
    string? ErrorMessage { get; set; }
    decimal? SuccessScore { get; set; }
    int? TestsPassed { get; set; }
    int? TestsFailed { get; set; }
    decimal? PerformanceDelta { get; set; }
    bool WasDeployed { get; set; }
    bool WasRolledBack { get; set; }
    DateTime StartedAt { get; set; }
    DateTime? CompletedAt { get; set; }
    DateTime? RolledBackAt { get; set; }
    ICollection<LearningMetric> LearningMetrics { get; set; }
}
