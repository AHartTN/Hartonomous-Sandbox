using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Autonomy
{
    /// <summary>
    /// OODA loop cycle history
    /// </summary>
    public class OodaCycleHistoryResponse
    {
        public required List<OodaCycleRecord> Cycles { get; init; }
        public required int TotalCycles { get; init; }
        public required double AvgLatencyImprovement { get; init; }
    }
}
