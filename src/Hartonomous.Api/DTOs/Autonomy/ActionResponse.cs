using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Autonomy
{
    /// <summary>
    /// Response from Act phase (monitoring only)
    /// </summary>
    public class ActionResponse
    {
        public required Guid AnalysisId { get; init; }
        public required int ExecutedActions { get; init; }
        public required int QueuedActions { get; init; }
        public required int FailedActions { get; init; }
        public required List<ActionResult> Results { get; init; }
        public required DateTime TimestampUtc { get; init; }
    }
}
