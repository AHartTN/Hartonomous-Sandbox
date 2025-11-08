namespace Hartonomous.Api.DTOs.Autonomy;

public class QueueStatusResponse
{
    public required string QueueName { get; init; }
    public required int MessageCount { get; init; }
    public required int ConversationCount { get; init; }
    public required DateTime? LastMessageUtc { get; init; }
}

/// <summary>
/// OODA loop cycle history
/// </summary>
