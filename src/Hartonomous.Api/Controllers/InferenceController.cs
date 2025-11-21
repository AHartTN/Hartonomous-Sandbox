using Hartonomous.Core.Interfaces.Inference;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// API endpoints for inference operations including job queue management and model scoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class InferenceController : ControllerBase
{
    private readonly IInferenceService _inferenceService;
    private readonly ILogger<InferenceController> _logger;

    public InferenceController(
        IInferenceService inferenceService,
        ILogger<InferenceController> logger)
    {
        _inferenceService = inferenceService;
        _logger = logger;
    }

    /// <summary>
    /// Submit an asynchronous inference job to the queue.
    /// </summary>
    /// <param name="request">Job submission details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inference job ID</returns>
    [HttpPost("jobs")]
    [ProducesResponseType(typeof(SubmitJobResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitJobResponse>> SubmitJob(
        [FromBody] SubmitJobRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("SubmitJob API called: ModelId {ModelId}", request.ModelId);

        var inferenceId = await _inferenceService.SubmitJobAsync(
            request.ModelId,
            request.InputData,
            request.Priority ?? 5,
            request.TenantId ?? 0,
            request.CorrelationId,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetJobStatus),
            new { inferenceId },
            new SubmitJobResponse(inferenceId, "Queued", DateTime.UtcNow));
    }

    /// <summary>
    /// Get the status of an inference job.
    /// </summary>
    /// <param name="inferenceId">Inference job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job status details</returns>
    [HttpGet("jobs/{inferenceId}")]
    [ProducesResponseType(typeof(JobStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobStatus>> GetJobStatus(
        [FromRoute] long inferenceId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetJobStatus API called: InferenceId {InferenceId}", inferenceId);

        try
        {
            var status = await _inferenceService.GetJobStatusAsync(inferenceId, cancellationToken);
            return Ok(status);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { message = $"Inference job {inferenceId} not found" });
        }
    }

    /// <summary>
    /// Update the status of an inference job (typically used by workers).
    /// </summary>
    /// <param name="inferenceId">Inference job ID</param>
    /// <param name="request">Status update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPatch("jobs/{inferenceId}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateJobStatus(
        [FromRoute] long inferenceId,
        [FromBody] UpdateJobStatusRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateJobStatus API called: InferenceId {InferenceId}, Status {Status}",
            inferenceId, request.Status);

        await _inferenceService.UpdateJobStatusAsync(
            inferenceId,
            request.Status,
            request.OutputData,
            request.ErrorMessage,
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Execute synchronous inference (blocks until complete).
    /// </summary>
    /// <param name="request">Inference request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inference results</returns>
    [HttpPost("run")]
    [ProducesResponseType(typeof(InferenceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InferenceResult>> RunInference(
        [FromBody] RunInferenceRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("RunInference API called: ModelId {ModelId}", request.ModelId);

        var result = await _inferenceService.RunAsync(
            request.ModelId,
            request.InputData,
            request.TenantId ?? 0,
            request.CorrelationId,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Score an atom using a specific model.
    /// </summary>
    /// <param name="request">Scoring request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Score result</returns>
    [HttpPost("score")]
    [ProducesResponseType(typeof(ScoreResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScoreResult>> Score(
        [FromBody] ScoreRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Score API called: ModelId {ModelId}, AtomId {AtomId}",
            request.ModelId, request.AtomId);

        var result = await _inferenceService.ScoreAsync(
            request.ModelId,
            request.AtomId,
            request.TenantId ?? 0,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Run ensemble inference using multiple models.
    /// </summary>
    /// <param name="request">Ensemble request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ensemble results</returns>
    [HttpPost("ensemble")]
    [ProducesResponseType(typeof(EnsembleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EnsembleResult>> Ensemble(
        [FromBody] EnsembleRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Ensemble API called: Models {Models}, Type {Type}",
            request.ModelIds, request.EnsembleType);

        var result = await _inferenceService.EnsembleAsync(
            request.ModelIds,
            request.EnsembleType ?? "voting",
            request.InputData ?? string.Empty,
            request.TenantId ?? 0,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Compare knowledge between two models.
    /// </summary>
    /// <param name="model1Id">First model ID</param>
    /// <param name="model2Id">Second model ID</param>
    /// <param name="topK">Number of top differences to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comparison results</returns>
    [HttpGet("compare")]
    [ProducesResponseType(typeof(ComparisonResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ComparisonResult>> CompareModels(
        [FromQuery, Required] int model1Id,
        [FromQuery, Required] int model2Id,
        [FromQuery] int topK = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CompareModels API called: Model1 {Model1}, Model2 {Model2}",
            model1Id, model2Id);

        var result = await _inferenceService.CompareModelsAsync(
            model1Id,
            model2Id,
            topK,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get inference history with optional filters.
    /// </summary>
    /// <param name="sessionId">Optional session ID filter</param>
    /// <param name="modelId">Optional model ID filter</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Inference history items</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<InferenceHistoryItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InferenceHistoryItem>>> GetHistory(
        [FromQuery] long? sessionId = null,
        [FromQuery] int? modelId = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetHistory API called: SessionId {SessionId}, ModelId {ModelId}, Limit {Limit}",
            sessionId, modelId, limit);

        var history = await _inferenceService.GetHistoryAsync(
            sessionId,
            modelId,
            limit,
            cancellationToken);

        return Ok(history);
    }
}

#region Request/Response DTOs

public record SubmitJobRequest(
    [Required] int ModelId,
    [Required] string InputData,
    int? Priority = 5,
    int? TenantId = 0,
    Guid? CorrelationId = null);

public record SubmitJobResponse(
    long InferenceId,
    string Status,
    DateTime SubmittedAt);

public record UpdateJobStatusRequest(
    [Required] string Status,
    string? OutputData = null,
    string? ErrorMessage = null);

public record RunInferenceRequest(
    [Required] int ModelId,
    [Required] string InputData,
    int? TenantId = 0,
    Guid? CorrelationId = null);

public record ScoreRequest(
    [Required] int ModelId,
    [Required] long AtomId,
    int? TenantId = 0);

public record EnsembleRequest(
    [Required] string ModelIds,
    string? EnsembleType = "voting",
    string? InputData = "",
    int? TenantId = 0);

#endregion
