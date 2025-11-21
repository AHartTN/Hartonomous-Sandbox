using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IPendingAction
{
    long ActionId { get; set; }
    string ActionType { get; set; }
    string? TargetEntity { get; set; }
    long? TargetId { get; set; }
    string? SqlStatement { get; set; }
    string? Description { get; set; }
    string? Parameters { get; set; }
    string? Metadata { get; set; }
    string Priority { get; set; }
    string Status { get; set; }
    string RiskLevel { get; set; }
    string? EstimatedImpact { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime? ApprovedUtc { get; set; }
    string? ApprovedBy { get; set; }
    DateTime? ExecutedUtc { get; set; }
    string? ResultJson { get; set; }
    string? ErrorMessage { get; set; }
}
