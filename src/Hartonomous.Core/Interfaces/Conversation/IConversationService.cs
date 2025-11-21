using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Conversation;

/// <summary>
/// Multi-turn conversation service for interactive AI dialogues.
/// Provides context-aware, stateful conversations using sp_Converse stored procedure.
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Engage in multi-turn conversation with context retention.
    /// Calls sp_Converse stored procedure.
    /// </summary>
    /// <param name="sessionId">Unique conversation session identifier</param>
    /// <param name="userMessage">User's message text</param>
    /// <param name="tenantId">Tenant identifier for multi-tenancy</param>
    /// <param name="maxTurns">Maximum conversation history to consider</param>
    /// <param name="temperature">Sampling temperature (0.0-2.0, default 1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with conversation context</returns>
    Task<ConversationResult> ConverseAsync(
        Guid sessionId,
        string userMessage,
        int tenantId = 0,
        int maxTurns = 10,
        float temperature = 1.0f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a new conversation session.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="initialContext">Optional initial context or system prompt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New session ID</returns>
    Task<Guid> StartSessionAsync(
        int tenantId = 0,
        string? initialContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get conversation history for a session.
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="limit">Maximum number of turns to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conversation history</returns>
    Task<IEnumerable<ConversationTurn>> GetHistoryAsync(
        Guid sessionId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear conversation history for a session.
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearHistoryAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a conversation turn.
/// </summary>
/// <param name="SessionId">Conversation session identifier</param>
/// <param name="TurnNumber">Sequential turn number in this session</param>
/// <param name="UserMessage">User's input message</param>
/// <param name="AssistantResponse">AI assistant's response</param>
/// <param name="Timestamp">When this turn occurred</param>
/// <param name="TokensUsed">Approximate tokens consumed</param>
/// <param name="ProcessingTimeMs">Processing duration in milliseconds</param>
public record ConversationResult(
    Guid SessionId,
    int TurnNumber,
    string UserMessage,
    string AssistantResponse,
    DateTime Timestamp,
    int? TokensUsed = null,
    int? ProcessingTimeMs = null);

/// <summary>
/// Single turn in a conversation.
/// </summary>
/// <param name="TurnNumber">Sequential turn number</param>
/// <param name="Role">Speaker role (user, assistant, system)</param>
/// <param name="Message">Message content</param>
/// <param name="Timestamp">When this turn occurred</param>
public record ConversationTurn(
    int TurnNumber,
    string Role,
    string Message,
    DateTime Timestamp);
