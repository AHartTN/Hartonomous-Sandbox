using System;

namespace Hartonomous.Api.DTOs.Audit;


public class PolicyStatus
{
    public string PolicyName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public string Summary { get; set; } = string.Empty;
}
