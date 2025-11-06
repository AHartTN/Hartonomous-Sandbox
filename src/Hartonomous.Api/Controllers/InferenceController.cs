using System.Collections.Generic;
using Hartonomous.Api.DTOs.Inference;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// API controller for submitting and monitoring inference jobs (text generation, ensemble inference).
/// Jobs are processed asynchronously by background workers.
/// </summary>
[Route("api/inference")]
public sealed class InferenceController : ApiControllerBase
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<InferenceController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceController"/> class.
    /// </summary>
    /// <param name="context">Database context for persisting inference requests.</param>
    /// <param name="logger">Logger for tracking submission and retrieval operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public InferenceController(
        HartonomousDbContext context,
        ILogger<InferenceController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submits an asynchronous text generation job with specified prompt, max tokens, and temperature.
    /// </summary>
    /// <param name="request">Request containing prompt and generation parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Accepted (202) with job ID and status URL if successful; BadRequest (400) for validation errors; InternalServerError (500) for infrastructure failures.</returns>
    [HttpPost("generate/text")]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<JobSubmittedResponse>>> GenerateTextAsync(
        [FromBody] GenerateTextRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<JobSubmittedResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(Failure<JobSubmittedResponse>(new[] { MissingField(nameof(request.Prompt)) }));
        }

        try
        {
            var inputData = System.Text.Json.JsonSerializer.Serialize(new
            {
                prompt = request.Prompt,
                maxTokens = Math.Clamp(request.MaxTokens, 1, 512),
                temperature = Math.Clamp(request.Temperature, 0.0, 2.0)
            });

            var job = new InferenceRequest
            {
                TaskType = "text_generation",
                InputData = inputData,
                Status = "Pending",
                CorrelationId = Guid.NewGuid().ToString()
            };

            _context.InferenceRequests.Add(job);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new JobSubmittedResponse
            {
                JobId = job.InferenceId,
                Status = "Pending",
                StatusUrl = $"/api/inference/jobs/{job.InferenceId}"
            };

            return Accepted($"/api/inference/jobs/{job.InferenceId}", Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit text generation job");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to submit job");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<JobSubmittedResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Submits an ensemble inference job that runs multiple models and aggregates results.
    /// </summary>
    /// <param name="request">Request containing input embedding, model IDs to use in ensemble, and task type.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Accepted (202) with job ID and status URL if successful; BadRequest (400) for validation errors; InternalServerError (500) for infrastructure failures.</returns>
    [HttpPost("ensemble")]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<JobSubmittedResponse>>> EnsembleInferenceAsync(
        [FromBody] EnsembleRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<JobSubmittedResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (request.Embedding == null || request.Embedding.Length == 0)
        {
            return BadRequest(Failure<JobSubmittedResponse>(new[] { MissingField(nameof(request.Embedding)) }));
        }

        if (request.ModelIds == null || request.ModelIds.Count == 0)
        {
            return BadRequest(Failure<JobSubmittedResponse>(new[] { ValidationError("At least one ModelId is required.", nameof(request.ModelIds)) }));
        }

        try
        {
            var inputData = System.Text.Json.JsonSerializer.Serialize(new
            {
                inputData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                    System.Text.Json.JsonSerializer.Serialize(request.Embedding))),
                modelIds = System.Text.Json.JsonSerializer.Serialize(request.ModelIds),
                taskType = request.TaskType ?? "classification"
            });

            var job = new InferenceRequest
            {
                TaskType = "ensemble",
                InputData = inputData,
                Status = "Pending",
                CorrelationId = Guid.NewGuid().ToString()
            };

            _context.InferenceRequests.Add(job);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new JobSubmittedResponse
            {
                JobId = job.InferenceId,
                Status = "Pending",
                StatusUrl = $"/api/inference/jobs/{job.InferenceId}"
            };

            return Accepted($"/api/inference/jobs/{job.InferenceId}", Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit ensemble job");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to submit job");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<JobSubmittedResponse>(new[] { error }));
        }
    }

    /// <summary>
    /// Retrieves the current status and results of an inference job by ID.
    /// </summary>
    /// <param name="jobId">The inference job ID to query.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>OK (200) with job status and output data if found; NotFound (404) if job ID doesn't exist.</returns>
    [HttpGet("jobs/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<JobStatusResponse>>> GetJobStatusAsync(long jobId, CancellationToken cancellationToken)
    {
        var job = await _context.InferenceRequests
            .FirstOrDefaultAsync(r => r.InferenceId == jobId, cancellationToken);

        if (job == null)
        {
            var error = ErrorDetailFactory.NotFound("job", jobId.ToString());
            return NotFound(Failure<JobStatusResponse>(new[] { error }));
        }

        var response = new JobStatusResponse
        {
            JobId = job.InferenceId,
            Status = job.Status ?? "Unknown",
            TaskType = job.TaskType,
            OutputData = job.OutputData,
            Confidence = job.Confidence,
            DurationMs = job.TotalDurationMs,
            CreatedAt = job.RequestTimestamp
        };

        return Ok(Success(response));
    }
}
