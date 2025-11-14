using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IWeightSnapshot
{
    long SnapshotId { get; set; }
    string SnapshotName { get; set; }
    int? ModelId { get; set; }
    DateTime SnapshotTime { get; set; }
    string? Description { get; set; }
    int WeightCount { get; set; }
    DateTime CreatedAt { get; set; }
    Model? Model { get; set; }
}
