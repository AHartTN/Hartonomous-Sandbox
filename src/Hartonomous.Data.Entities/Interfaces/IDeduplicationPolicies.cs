using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IDeduplicationPolicies
{
    int DeduplicationPolicyId { get; set; }
    string PolicyName { get; set; }
    double? SemanticThreshold { get; set; }
    double? SpatialThreshold { get; set; }
    string? Metadata { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
}
