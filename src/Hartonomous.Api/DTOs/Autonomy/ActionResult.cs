using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Autonomy
{
    public class ActionResult
    {
        public required Guid HypothesisId { get; init; }
        public required string HypothesisType { get; init; }
        public required string ActionStatus { get; init; }
        public required Dictionary<string, object> ExecutedActions { get; init; }
        public required int ExecutionTimeMs { get; init; }
        public string? ErrorMessage { get; init; }
    }
}
