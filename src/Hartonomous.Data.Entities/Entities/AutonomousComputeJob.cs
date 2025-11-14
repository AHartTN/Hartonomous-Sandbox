using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AutonomousComputeJob : IAutonomousComputeJob
{
    public Guid JobId { get; set; }

    public string JobType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string JobParameters { get; set; } = null!;

    public string? CurrentState { get; set; }

    public string? Results { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
