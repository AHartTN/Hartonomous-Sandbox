using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IWeightSnapshots
{
    long SnapshotId { get; set; }
    string SnapshotName { get; set; }
    int? ModelId { get; set; }
    DateTime SnapshotTime { get; set; }
    string? Description { get; set; }
    int WeightCount { get; set; }
    DateTime CreatedAt { get; set; }
    Models? Model { get; set; }
}
