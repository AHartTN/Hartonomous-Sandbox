using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class EventAtom : IEventAtom
{
    public int Id { get; set; }

    public int StreamId { get; set; }

    public string EventType { get; set; } = null!;

    public long CentroidAtomId { get; set; }

    public double AverageWeight { get; set; }

    public int ClusterSize { get; set; }

    public int ClusterId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Atom CentroidAtom { get; set; } = null!;

    public virtual StreamOrchestrationResult Stream { get; set; } = null!;
}
