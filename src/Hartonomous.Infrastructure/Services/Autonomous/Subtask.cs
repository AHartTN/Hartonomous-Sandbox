namespace Hartonomous.Infrastructure.Services.Autonomous
{
    public sealed class Subtask
    {
        public required SubtaskType Type { get; init; }
        public required string Description { get; init; }
        public required string Parameters { get; init; }
    }
}
