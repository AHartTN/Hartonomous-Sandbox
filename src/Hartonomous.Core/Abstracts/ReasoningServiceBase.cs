using Hartonomous.Core.Interfaces.Reasoning;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Base class for reasoning service implementations providing common validation
/// and telemetry patterns specific to AI reasoning operations.
/// </summary>
/// <typeparam name="TService">The concrete reasoning service type.</typeparam>
public abstract class ReasoningServiceBase<TService> : ServiceBase<TService>, IReasoningService
    where TService : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReasoningServiceBase{TService}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected ReasoningServiceBase(ILogger<TService> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    public async Task<ReasoningResult> ExecuteChainOfThoughtAsync(
        long sessionId,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        ValidateRange(sessionId, nameof(sessionId), 1, long.MaxValue);
        ValidateNotNullOrWhiteSpace(prompt, nameof(prompt));

        if (prompt.Length > 10000)
        {
            throw new ArgumentException("Prompt exceeds maximum length of 10,000 characters.", nameof(prompt));
        }

        return await ExecuteWithTelemetryAsync(
            $"ChainOfThought (SessionId={sessionId})",
            async () => await ExecuteChainOfThoughtInternalAsync(sessionId, prompt, cancellationToken)
                .ConfigureAwait(false),
            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ReasoningResult> ExecuteTreeOfThoughtAsync(
        long sessionId,
        string prompt,
        int maxBranches = 3,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        ValidateRange(sessionId, nameof(sessionId), 1, long.MaxValue);
        ValidateNotNullOrWhiteSpace(prompt, nameof(prompt));
        ValidateRange(maxBranches, nameof(maxBranches), 1, 10);

        if (prompt.Length > 10000)
        {
            throw new ArgumentException("Prompt exceeds maximum length of 10,000 characters.", nameof(prompt));
        }

        return await ExecuteWithTelemetryAsync(
            $"TreeOfThought (SessionId={sessionId}, MaxBranches={maxBranches})",
            async () => await ExecuteTreeOfThoughtInternalAsync(sessionId, prompt, maxBranches, cancellationToken)
                .ConfigureAwait(false),
            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReasoningResult>> GetSessionHistoryAsync(
        long sessionId,
        CancellationToken cancellationToken = default)
    {
        // Validate inputs
        ValidateRange(sessionId, nameof(sessionId), 1, long.MaxValue);

        return await ExecuteWithTelemetryAsync(
            $"GetSessionHistory (SessionId={sessionId})",
            async () => await GetSessionHistoryInternalAsync(sessionId, cancellationToken)
                .ConfigureAwait(false),
            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Internal implementation of Chain-of-Thought reasoning.
    /// Override in derived classes (SqlReasoningService, MockReasoningService).
    /// </summary>
    protected abstract Task<ReasoningResult> ExecuteChainOfThoughtInternalAsync(
        long sessionId,
        string prompt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Internal implementation of Tree-of-Thought reasoning.
    /// Override in derived classes (SqlReasoningService, MockReasoningService).
    /// </summary>
    protected abstract Task<ReasoningResult> ExecuteTreeOfThoughtInternalAsync(
        long sessionId,
        string prompt,
        int maxBranches,
        CancellationToken cancellationToken);

    /// <summary>
    /// Internal implementation of session history retrieval.
    /// Override in derived classes (SqlReasoningService, MockReasoningService).
    /// </summary>
    protected abstract Task<IEnumerable<ReasoningResult>> GetSessionHistoryInternalAsync(
        long sessionId,
        CancellationToken cancellationToken);
}
