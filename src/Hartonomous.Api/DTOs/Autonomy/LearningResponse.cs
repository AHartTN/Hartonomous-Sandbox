using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Autonomy
{
    /// <summary>
    /// Response from Learn phase (monitoring only)
    /// </summary>
    public class LearningResponse
    {
        public required Guid AnalysisId { get; init; }
        public required bool LearningCycleComplete { get; init; }
        public required PerformanceMetrics PerformanceMetrics { get; init; }
        public required ActionOutcomeSummary ActionOutcomes { get; init; }
        public required List<ActionOutcome> Outcomes { get; init; }
        public required DateTime TimestampUtc { get; init; }
    }
}
