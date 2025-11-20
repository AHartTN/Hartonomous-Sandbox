namespace Hartonomous.Core.Interfaces.Reasoning;

/// <summary>
/// Represents a service that executes AI reasoning operations using different reasoning strategies.
/// Implementations may use SQL stored procedures, mock data, or external ML services.
/// </summary>
public interface IReasoningService
{
    /// <summary>
    /// Executes Chain-of-Thought reasoning on the specified session.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the reasoning session.</param>
    /// <param name="prompt">The input prompt or query to reason about.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The reasoning result including intermediate steps and final conclusion.</returns>
    Task<ReasoningResult> ExecuteChainOfThoughtAsync(
        long sessionId,
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes Tree-of-Thought reasoning with branching exploration paths.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the reasoning session.</param>
    /// <param name="prompt">The input prompt or query to reason about.</param>
    /// <param name="maxBranches">Maximum number of reasoning branches to explore.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The reasoning result including explored branches and best path.</returns>
    Task<ReasoningResult> ExecuteTreeOfThoughtAsync(
        long sessionId,
        string prompt,
        int maxBranches = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves reasoning history for a specific session.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the reasoning session.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of reasoning results for the session.</returns>
    Task<IEnumerable<ReasoningResult>> GetSessionHistoryAsync(
        long sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a reasoning operation.
/// </summary>
public sealed class ReasoningResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this reasoning result.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the session ID this result belongs to.
    /// </summary>
    public long SessionId { get; set; }

    /// <summary>
    /// Gets or sets the reasoning strategy used (e.g., "ChainOfThought", "TreeOfThought").
    /// </summary>
    public required string Strategy { get; set; }

    /// <summary>
    /// Gets or sets the input prompt that was reasoned about.
    /// </summary>
    public required string Prompt { get; set; }

    /// <summary>
    /// Gets or sets the final conclusion or answer.
    /// </summary>
    public required string Conclusion { get; set; }

    /// <summary>
    /// Gets or sets the intermediate reasoning steps as JSON.
    /// </summary>
    public string? IntermediateSteps { get; set; }

    /// <summary>
    /// Gets or sets the confidence score (0.0 to 1.0).
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when reasoning was executed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}
