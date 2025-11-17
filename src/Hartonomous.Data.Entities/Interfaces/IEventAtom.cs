using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IEventAtom
{
    int Id { get; set; }
    int StreamId { get; set; }
    string EventType { get; set; }
    long CentroidAtomId { get; set; }
    double AverageWeight { get; set; }
    int ClusterSize { get; set; }
    int ClusterId { get; set; }
    DateTime CreatedAt { get; set; }
    Atom CentroidAtom { get; set; }
    StreamOrchestrationResult Stream { get; set; }
}
