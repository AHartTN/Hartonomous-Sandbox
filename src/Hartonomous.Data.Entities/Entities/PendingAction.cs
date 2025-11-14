using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class PendingAction : IPendingAction
{
    public long ActionId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? SqlStatement { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public string RiskLevel { get; set; } = null!;

    public string? EstimatedImpact { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? ApprovedUtc { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ExecutedUtc { get; set; }

    public string? ResultJson { get; set; }

    public string? ErrorMessage { get; set; }
}
