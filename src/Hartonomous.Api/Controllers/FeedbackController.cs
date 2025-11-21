using Asp.Versioning;
using Hartonomous.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Feedback endpoints for RLHF (Reinforcement Learning from Human Feedback).
/// Allows users to rate inference quality and improve the reasoning system.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reasoning")]
[Authorize(Policy = "ApiUser")]
[EnableRateLimiting("standard")]
public class FeedbackController : ControllerBase
{
    private readonly HartonomousDbContext _dbContext;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        HartonomousDbContext dbContext,
        ILogger<FeedbackController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Submit feedback for an inference result to improve future reasoning.
    /// </summary>
    /// <param name="inferenceId">The inference request ID to provide feedback for.</param>
    /// <param name="request">The feedback data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of feedback processing.</returns>
    /// <response code="200">Feedback processed successfully.</response>
    /// <response code="400">Invalid feedback data.</response>
    /// <response code="404">Inference request not found.</response>
    [HttpPost("{inferenceId}/feedback")]
    [ProducesResponseType(typeof(FeedbackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitFeedback(
        [FromRoute] long inferenceId,
        [FromBody] FeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing feedback for inference {InferenceId} with rating {Rating}",
            inferenceId, request.Rating);

        if (request.Rating < 1 || request.Rating > 5)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Rating",
                Detail = "Rating must be between 1 and 5.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var userId = User.Identity?.Name ?? "anonymous";

            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC dbo.sp_ProcessFeedback @InferenceId, @Rating, @Comments, @UserId";
            command.Parameters.Add(new SqlParameter("@InferenceId", inferenceId));
            command.Parameters.Add(new SqlParameter("@Rating", request.Rating));
            command.Parameters.Add(new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value));
            command.Parameters.Add(new SqlParameter("@UserId", userId));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var success = reader.GetBoolean(reader.GetOrdinal("Success"));
                var message = reader.GetString(reader.GetOrdinal("Message"));
                var affectedRelations = reader.GetInt32(reader.GetOrdinal("AffectedRelations"));

                if (!success)
                {
                    if (message.Contains("not found"))
                    {
                        return NotFound(new ProblemDetails
                        {
                            Title = "Inference Not Found",
                            Detail = message,
                            Status = StatusCodes.Status404NotFound
                        });
                    }

                    return BadRequest(new ProblemDetails
                    {
                        Title = "Feedback Processing Failed",
                        Detail = message,
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                return Ok(new FeedbackResponse
                {
                    Success = true,
                    Message = message,
                    AffectedRelations = affectedRelations,
                    InferenceId = inferenceId,
                    Rating = request.Rating
                });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Unexpected Error",
                Detail = "No result returned from feedback processing.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing feedback for inference {InferenceId}", inferenceId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing feedback.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get aggregated feedback statistics for a session.
    /// </summary>
    /// <param name="sessionId">The session ID to get feedback for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Feedback statistics for the session.</returns>
    [HttpGet("sessions/{sessionId}/feedback/stats")]
    [ProducesResponseType(typeof(FeedbackStats), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeedbackStats(
        [FromRoute] long sessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving feedback stats for session {SessionId}", sessionId);

        var stats = await _dbContext.Database
            .SqlQuery<FeedbackStats>($@"
                SELECT
                    COUNT(*) AS TotalFeedback,
                    AVG(CAST(f.Rating AS FLOAT)) AS AverageRating,
                    SUM(CASE WHEN f.Rating >= 4 THEN 1 ELSE 0 END) AS PositiveCount,
                    SUM(CASE WHEN f.Rating <= 2 THEN 1 ELSE 0 END) AS NegativeCount,
                    SUM(CASE WHEN f.Rating = 3 THEN 1 ELSE 0 END) AS NeutralCount
                FROM dbo.InferenceFeedback f
                INNER JOIN dbo.InferenceRequests ir ON f.InferenceRequestId = ir.InferenceRequestId
                WHERE ir.SessionId = {sessionId}")
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(stats ?? new FeedbackStats());
    }
}

/// <summary>
/// Request model for submitting feedback.
/// </summary>
public class FeedbackRequest
{
    /// <summary>
    /// Rating from 1 (very poor) to 5 (excellent).
    /// </summary>
    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    /// <summary>
    /// Optional comments explaining the rating.
    /// </summary>
    [MaxLength(2000)]
    public string? Comments { get; set; }
}

/// <summary>
/// Response model for feedback submission.
/// </summary>
public class FeedbackResponse
{
    /// <summary>
    /// Whether feedback was processed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Descriptive message about the operation.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Number of relationship weights adjusted.
    /// </summary>
    public int AffectedRelations { get; set; }

    /// <summary>
    /// The inference ID that received feedback.
    /// </summary>
    public long InferenceId { get; set; }

    /// <summary>
    /// The rating that was submitted.
    /// </summary>
    public int Rating { get; set; }
}

/// <summary>
/// Aggregated feedback statistics.
/// </summary>
public class FeedbackStats
{
    /// <summary>
    /// Total number of feedback submissions.
    /// </summary>
    public int TotalFeedback { get; set; }

    /// <summary>
    /// Average rating across all feedback.
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Count of positive ratings (4-5).
    /// </summary>
    public int PositiveCount { get; set; }

    /// <summary>
    /// Count of negative ratings (1-2).
    /// </summary>
    public int NegativeCount { get; set; }

    /// <summary>
    /// Count of neutral ratings (3).
    /// </summary>
    public int NeutralCount { get; set; }
}
