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

[Route("api/inference")]
public sealed class InferenceController : ApiControllerBase
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<InferenceController> _logger;

    public InferenceController(
        HartonomousDbContext context,
        ILogger<InferenceController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
