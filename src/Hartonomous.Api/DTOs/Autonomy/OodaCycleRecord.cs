using System;

namespace Hartonomous.Api.DTOs.Autonomy
{
    public class OodaCycleRecord
    {
        public required Guid AnalysisId { get; init; }
        public required DateTime StartTimeUtc { get; init; }
        public required DateTime? EndTimeUtc { get; init; }
        public required int HypothesesGenerated { get; init; }
        public required int ActionsExecuted { get; init; }
        public required double? LatencyImprovement { get; init; }
        public required string Status { get; init; }
    }
}
