using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Autonomy
{
    /// <summary>
    /// Response from Hypothesize phase (monitoring only)
    /// </summary>
    public class HypothesisResponse
    {
        public required Guid AnalysisId { get; init; }
        public required int HypothesesGenerated { get; init; }
        public required List<Hypothesis> Hypotheses { get; init; }
        public required DateTime TimestampUtc { get; init; }
    }
}
