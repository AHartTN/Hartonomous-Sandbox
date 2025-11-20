using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Audit;


public class ComplianceFramework
{
    public string Framework { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastAudit { get; set; }
    public DateTime NextAudit { get; set; }
    public List<ControlStatus> Controls { get; set; } = new();
}
