using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAutonomousComputeJob
{
    Guid JobId { get; set; }
    string JobType { get; set; }
    string Status { get; set; }
    string JobParameters { get; set; }
    string? CurrentState { get; set; }
    string? Results { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    DateTime? CompletedAt { get; set; }
}
