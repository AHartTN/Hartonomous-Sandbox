using System;
using System.Collections.Generic;
using System.Linq;

namespace Hartonomous.Infrastructure.Services.Autonomous
{
    public sealed class TaskExecutionResult
    {
        public required string OriginalPrompt { get; init; }
        public List<Subtask> Subtasks { get; set; } = new();
        public List<SubtaskResult> SubtaskResults { get; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }
}
