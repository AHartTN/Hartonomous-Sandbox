namespace Hartonomous.Api.DTOs.Autonomy
{
    public class PerformanceMetrics
    {
        public required double BaselineLatencyMs { get; init; }
        public required double CurrentLatencyMs { get; init; }
        public required double LatencyImprovement { get; init; }
        public required double ThroughputChange { get; init; }
    }
}
