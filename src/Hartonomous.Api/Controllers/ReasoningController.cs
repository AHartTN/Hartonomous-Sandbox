using Hartonomous.Core.Interfaces.Reasoning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Advanced AI reasoning endpoints using Chain of Thought and Tree of Thought algorithms.
/// Demonstrates model atomization and semantic-first architecture.
/// </summary>
[EnableRateLimiting("query")]
public class ReasoningController : ApiControllerBase
{
    private readonly IReasoningService _reasoningService;

    public ReasoningController(
        IReasoningService reasoningService,
        ILogger<ReasoningController> logger)
        : base(logger)
    {
        _reasoningService = reasoningService ?? throw new ArgumentNullException(nameof(reasoningService));
    }

    /// <summary>
    /// Execute Chain of Thought reasoning on a prompt.
    /// Returns step-by-step reasoning process with intermediate thoughts.
    /// </summary>
    /// <param name="request">Chain of Thought request with prompt and optional session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reasoning result with confidence score and intermediate steps</returns>
    /// <response code="200">Successfully executed Chain of Thought reasoning</response>
    /// <response code="400">Invalid request (empty prompt, invalid session ID)</response>
    /// <response code="500">Internal server error during reasoning</response>
    [HttpPost("chain-of-thought")]
    [ProducesResponseType(typeof(ReasoningResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteChainOfThought(
        [FromBody] ChainOfThoughtRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid prompt",
                Detail = "Prompt cannot be null or whitespace",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            Logger.LogInformation(
                "Executing Chain of Thought reasoning for session {SessionId}, prompt length: {Length}",
                request.SessionId,
                request.Prompt.Length);

            var result = await _reasoningService.ExecuteChainOfThoughtAsync(
                request.SessionId,
                request.Prompt,
                cancellationToken);

            Logger.LogInformation(
                "Chain of Thought completed. Confidence: {Confidence}, Execution time: {ExecutionTime}ms",
                result.ConfidenceScore,
                result.ExecutionTimeMs);

            return SuccessResult(result);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Chain of Thought reasoning was cancelled");
            return ErrorResult("Request was cancelled", 499);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing Chain of Thought reasoning");
            return ErrorResult("An error occurred while executing Chain of Thought reasoning", 500);
        }
    }

    /// <summary>
    /// Execute Tree of Thought reasoning with multiple reasoning branches.
    /// Explores multiple solution paths and returns the optimal reasoning path.
    /// </summary>
    /// <param name="request">Tree of Thought request with prompt, max branches, and optional session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reasoning result with the best path and confidence score</returns>
    /// <response code="200">Successfully executed Tree of Thought reasoning</response>
    /// <response code="400">Invalid request (empty prompt, invalid branches)</response>
    /// <response code="500">Internal server error during reasoning</response>
    [HttpPost("tree-of-thought")]
    [ProducesResponseType(typeof(ReasoningResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteTreeOfThought(
        [FromBody] TreeOfThoughtRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return ErrorResult("Prompt cannot be null or whitespace", 400);
        }

        if (request.MaxBranches < 1)
        {
            return ErrorResult("MaxBranches must be at least 1", 400);
        }

        try
        {
            Logger.LogInformation(
                "Executing Tree of Thought reasoning for session {SessionId}, prompt length: {Length}, max branches: {MaxBranches}",
                request.SessionId,
                request.Prompt.Length,
                request.MaxBranches);

            var result = await _reasoningService.ExecuteTreeOfThoughtAsync(
                request.SessionId,
                request.Prompt,
                request.MaxBranches,
                cancellationToken);

            Logger.LogInformation(
                "Tree of Thought completed. Confidence: {Confidence}, Execution time: {ExecutionTime}ms",
                result.ConfidenceScore,
                result.ExecutionTimeMs);

            return SuccessResult(result);
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Tree of Thought reasoning was cancelled");
            return ErrorResult("Request was cancelled", 499);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing Tree of Thought reasoning");
            return ErrorResult("An error occurred while executing Tree of Thought reasoning", 500);
        }
    }

    /// <summary>
    /// Get the reasoning history for a specific session.
    /// Returns all reasoning operations performed in the session in chronological order.
    /// </summary>
    /// <param name="sessionId">Session ID to retrieve history for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reasoning results from the session</returns>
    /// <response code="200">Successfully retrieved session history</response>
    /// <response code="400">Invalid session ID</response>
    /// <response code="404">Session not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("sessions/{sessionId}/history")]
    [ProducesResponseType(typeof(IEnumerable<ReasoningResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSessionHistory(
        [FromRoute] long sessionId,
        CancellationToken cancellationToken = default)
    {
        if (sessionId <= 0)
        {
            return ErrorResult("Session ID must be greater than 0", 400);
        }

        try
        {
            Logger.LogInformation("Retrieving history for session {SessionId}", sessionId);

            var history = await _reasoningService.GetSessionHistoryAsync(sessionId, cancellationToken);

            if (history == null || !history.Any())
            {
                Logger.LogInformation("No history found for session {SessionId}", sessionId);
                return ErrorResult($"No history found for session {sessionId}", 404);
            }

            Logger.LogInformation(
                "Retrieved {Count} reasoning operations for session {SessionId}",
                history.Count(),
                sessionId);

            return SuccessResult(history);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving session history for session {SessionId}", sessionId);
            return ErrorResult("An error occurred while retrieving session history", 500);
        }
    }
}
