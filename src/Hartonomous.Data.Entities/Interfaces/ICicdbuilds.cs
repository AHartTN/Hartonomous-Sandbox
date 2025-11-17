using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ICicdbuilds
{
    long BuildId { get; set; }
    string CommitHash { get; set; }
    string BranchName { get; set; }
    string? BuildNumber { get; set; }
    string Status { get; set; }
    DateTime? StartedAt { get; set; }
    DateTime? CompletedAt { get; set; }
    int? DurationMs { get; set; }
    string? BuildAgent { get; set; }
    string? TriggerType { get; set; }
    string? BuildLogs { get; set; }
    string? ArtifactUrl { get; set; }
    int? TestsPassed { get; set; }
    int? TestsFailed { get; set; }
    decimal? CodeCoverage { get; set; }
    DateTime? DeployedAt { get; set; }
    string? DeploymentStatus { get; set; }
    DateTime CreatedAt { get; set; }
}
