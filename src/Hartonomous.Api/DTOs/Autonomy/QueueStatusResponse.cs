using System;

namespace Hartonomous.Api.DTOs.Autonomy
{
    /// <summary>
    /// Service Broker queue status for monitoring
    /// </summary>
    public class QueueStatusResponse
    {
        public required string QueueName { get; init; }
        public required int MessageCount { get; init; }
        public required int ConversationCount { get; init; }
        public required DateTime? LastMessageUtc { get; init; }
    }
}
