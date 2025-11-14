using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITenantSecurityPolicy
{
    int PolicyId { get; set; }
    string TenantId { get; set; }
    string PolicyName { get; set; }
    string PolicyType { get; set; }
    string PolicyRules { get; set; }
    bool IsActive { get; set; }
    DateTime? EffectiveFrom { get; set; }
    DateTime? EffectiveTo { get; set; }
    DateTime CreatedUtc { get; set; }
    DateTime? UpdatedUtc { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}
