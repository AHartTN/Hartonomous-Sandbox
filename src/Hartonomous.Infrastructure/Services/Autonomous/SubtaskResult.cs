using System;

namespace Hartonomous.Infrastructure.Services.Autonomous
{
    public sealed class SubtaskResult
    {
        public required Subtask Subtask { get; init; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OutputData { get; set; }
        public int AtomsCreated { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
