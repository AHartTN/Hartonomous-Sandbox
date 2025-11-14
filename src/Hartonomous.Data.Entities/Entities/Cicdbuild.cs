using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class Cicdbuild : ICicdbuild
{
    public long BuildId { get; set; }

    public string CommitHash { get; set; } = null!;

    public string BranchName { get; set; } = null!;

    public string? BuildNumber { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? DurationMs { get; set; }

    public string? BuildAgent { get; set; }

    public string? TriggerType { get; set; }

    public string? BuildLogs { get; set; }

    public string? ArtifactUrl { get; set; }

    public int? TestsPassed { get; set; }

    public int? TestsFailed { get; set; }

    public decimal? CodeCoverage { get; set; }

    public DateTime? DeployedAt { get; set; }

    public string? DeploymentStatus { get; set; }

    public DateTime CreatedAt { get; set; }
}
