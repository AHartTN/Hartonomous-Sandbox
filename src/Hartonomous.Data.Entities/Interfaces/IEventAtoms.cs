using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IEventAtoms
{
    int Id { get; set; }
    int StreamId { get; set; }
    string EventType { get; set; }
    long CentroidAtomId { get; set; }
    double AverageWeight { get; set; }
    int ClusterSize { get; set; }
    int ClusterId { get; set; }
    DateTime CreatedAt { get; set; }
    Atoms CentroidAtom { get; set; }
    StreamOrchestrationResults Stream { get; set; }
}
