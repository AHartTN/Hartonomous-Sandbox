using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Autonomy
{
    public class Hypothesis
    {
        public required Guid HypothesisId { get; init; }
        public required string HypothesisType { get; init; }
        public required int Priority { get; init; }
        public required string Description { get; init; }
        public required Dictionary<string, object> ExpectedImpact { get; init; }
        public required List<string> RequiredActions { get; init; }
    }
}
