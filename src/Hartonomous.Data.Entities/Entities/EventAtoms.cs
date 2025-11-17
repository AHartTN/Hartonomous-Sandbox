using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class EventAtoms : IEventAtoms
{
    public int Id { get; set; }

    public int StreamId { get; set; }

    public string EventType { get; set; } = null!;

    public long CentroidAtomId { get; set; }

    public double AverageWeight { get; set; }

    public int ClusterSize { get; set; }

    public int ClusterId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atoms CentroidAtom { get; set; } = null!;

    public virtual StreamOrchestrationResults Stream { get; set; } = null!;
}
