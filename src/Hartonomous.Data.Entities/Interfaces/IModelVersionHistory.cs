using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IModelVersionHistory
{
    long VersionHistoryId { get; set; }
    int ModelId { get; set; }
    string VersionTag { get; set; }
    string? VersionHash { get; set; }
    string? ChangeDescription { get; set; }
    long? ParentVersionId { get; set; }
    string? PerformanceMetrics { get; set; }
    string? CreatedBy { get; set; }
    DateTime CreatedAt { get; set; }
    int TenantId { get; set; }
    ICollection<ModelVersionHistory> InverseParentVersion { get; set; }
    Model Model { get; set; }
    ModelVersionHistory? ParentVersion { get; set; }
}
