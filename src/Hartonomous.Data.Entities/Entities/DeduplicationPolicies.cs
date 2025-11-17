using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class DeduplicationPolicies : IDeduplicationPolicies
{
    public int DeduplicationPolicyId { get; set; }

    public string PolicyName { get; set; } = null!;

    public double? SemanticThreshold { get; set; }

    public double? SpatialThreshold { get; set; }

    public string? Metadata { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
