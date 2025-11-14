using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class ProvenanceAuditResult : IProvenanceAuditResult
{
    public int Id { get; set; }

    public DateTime AuditPeriodStart { get; set; }

    public DateTime AuditPeriodEnd { get; set; }

    public string? Scope { get; set; }

    public int TotalOperations { get; set; }

    public int ValidOperations { get; set; }

    public int WarningOperations { get; set; }

    public int FailedOperations { get; set; }

    public double? AverageValidationScore { get; set; }

    public double? AverageSegmentCount { get; set; }

    public string? Anomalies { get; set; }

    public int AuditDurationMs { get; set; }

    public DateTime AuditedAt { get; set; }
}
