using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class WeightSnapshot : IWeightSnapshot
{
    public long SnapshotId { get; set; }

    public string SnapshotName { get; set; } = null!;

    public int? ModelId { get; set; }

    public DateTime SnapshotTime { get; set; }

    public string? Description { get; set; }

    public int WeightCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Model? Model { get; set; }
}
