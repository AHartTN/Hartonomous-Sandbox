using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IStreamOrchestrationResult
{
    int Id { get; set; }
    string SensorType { get; set; }
    DateTime TimeWindowStart { get; set; }
    DateTime TimeWindowEnd { get; set; }
    string AggregationLevel { get; set; }
    byte[]? ComponentStream { get; set; }
    int? ComponentCount { get; set; }
    int DurationMs { get; set; }
    DateTime CreatedAt { get; set; }
    ICollection<EventAtom> EventAtoms { get; set; }
    ICollection<EventGenerationResult> EventGenerationResults { get; set; }
}
