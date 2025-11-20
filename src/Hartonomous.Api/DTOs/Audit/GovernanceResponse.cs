using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Audit;


public class GovernanceResponse
{
    public List<ComplianceFramework> ComplianceFrameworks { get; set; } = new();
    public List<PolicyStatus> Policies { get; set; } = new();
    public bool DemoMode { get; set; }
}
