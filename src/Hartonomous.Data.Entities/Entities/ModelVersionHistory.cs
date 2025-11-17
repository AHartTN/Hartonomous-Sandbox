using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class ModelVersionHistory : IModelVersionHistory
{
    public long VersionHistoryId { get; set; }

    public int ModelId { get; set; }

    public string VersionTag { get; set; } = null!;

    public string? VersionHash { get; set; }

    public string? ChangeDescription { get; set; }

    public long? ParentVersionId { get; set; }

    public string? PerformanceMetrics { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int TenantId { get; set; }

    public virtual ICollection<ModelVersionHistory> InverseParentVersion { get; set; } = new List<ModelVersionHistory>();

    public virtual Models Model { get; set; } = null!;

    public virtual ModelVersionHistory? ParentVersion { get; set; }
}
