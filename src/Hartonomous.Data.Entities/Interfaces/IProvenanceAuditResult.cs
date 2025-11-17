using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IProvenanceAuditResult
{
    int Id { get; set; }
    DateTime AuditPeriodStart { get; set; }
    DateTime AuditPeriodEnd { get; set; }
    string? Scope { get; set; }
    int TotalOperations { get; set; }
    int ValidOperations { get; set; }
    int WarningOperations { get; set; }
    int FailedOperations { get; set; }
    double? AverageValidationScore { get; set; }
    double? AverageSegmentCount { get; set; }
    string? Anomalies { get; set; }
    int AuditDurationMs { get; set; }
    DateTime AuditedAt { get; set; }
}
