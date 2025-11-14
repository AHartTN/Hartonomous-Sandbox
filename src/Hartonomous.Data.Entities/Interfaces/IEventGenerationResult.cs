using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IEventGenerationResult
{
    int Id { get; set; }
    int StreamId { get; set; }
    string EventType { get; set; }
    double Threshold { get; set; }
    string ClusteringMethod { get; set; }
    int EventsGenerated { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
    StreamOrchestrationResult Stream { get; set; }
}
