using System.Collections.Generic;
using Hartonomous.Api.DTOs.Inference;
using Hartonomous.Core.Interfaces;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Hartonomous.Api.Controllers;

[Route("api/inference")]
public sealed class InferenceController : ApiControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IInferenceService _inferenceService;
    private readonly ILogger<InferenceController> _logger;

    public InferenceController(
        IEmbeddingService embeddingService,
        IInferenceService inferenceService,
        ILogger<InferenceController> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("generate/text")]
    [ProducesResponseType(typeof(ApiResponse<GenerateTextResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GenerateTextResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GenerateTextResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<GenerateTextResponse>>> GenerateTextAsync(
        [FromBody] GenerateTextRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<GenerateTextResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(Failure<GenerateTextResponse>(new[] { MissingField(nameof(request.Prompt)) }));
        }

        try
        {
            var promptEmbedding = await _embeddingService
                .EmbedTextAsync(request.Prompt, cancellationToken)
                .ConfigureAwait(false);

            var generation = await _inferenceService
                .GenerateViaSpatialAsync(
                    promptEmbedding,
                    Math.Clamp(request.MaxTokens, 1, 512),
                    (float)Math.Clamp(request.Temperature, 0.0, 2.0),
                    cancellationToken)
                .ConfigureAwait(false);

            var response = new GenerateTextResponse
            {
                InferenceId = generation.InferenceId,
                StreamId = Guid.NewGuid(),
                OriginalPrompt = request.Prompt,
                GeneratedText = generation.GeneratedText,
                TokensGenerated = generation.TokenCount,
                DurationMs = 0
            };

            var metadata = new Dictionary<string, object?>
            {
                ["tokenCount"] = generation.TokenCount,
                ["averageConfidence"] = generation.AverageConfidence,
                ["promptLength"] = request.Prompt.Length
            };

            return Ok(Success(response, metadata));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid generate request received.");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<GenerateTextResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text generation failed.");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while generating text.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<GenerateTextResponse>(new[] { error }));
        }
    }

    [HttpPost("ensemble")]
    [ProducesResponseType(typeof(ApiResponse<EnsembleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EnsembleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EnsembleResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EnsembleResponse>>> EnsembleInferenceAsync(
        [FromBody] EnsembleRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<EnsembleResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (request.Embedding == null || request.Embedding.Length == 0)
        {
            return BadRequest(Failure<EnsembleResponse>(new[] { MissingField(nameof(request.Embedding)) }));
        }

        if (request.ModelIds == null || request.ModelIds.Count == 0)
        {
            return BadRequest(Failure<EnsembleResponse>(new[] { ValidationError("At least one ModelId is required.", nameof(request.ModelIds)) }));
        }

        try
        {
            _logger.LogInformation("Starting ensemble inference with {ModelCount} models for task {TaskType}", 
                request.ModelIds.Count, 
                request.TaskType ?? "classification");

            // Convert embedding to string input (base64 encoded for now)
            var inputData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                System.Text.Json.JsonSerializer.Serialize(request.Embedding)));

            var result = await _inferenceService.EnsembleInferenceAsync(
                inputData,
                request.ModelIds,
                null, // weights
                cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Ensemble inference completed: InferenceId={InferenceId}, Contributions={ContributionCount}", 
                result.InferenceId, 
                result.ModelContributions?.Count ?? 0);

            var response = new EnsembleResponse
            {
                InferenceId = result.InferenceId,
                Results = result.ModelContributions?.Select(mc => new EnsembleResult
                {
                    AtomId = 0, // Not available from this result type
                    Modality = mc.ModelName,
                    Subtype = mc.IndividualOutput,
                    EnsembleScore = mc.ConfidenceScore,
                    ModelCount = result.ModelContributions.Count,
                    IsConsensus = mc.Weight > 0.5f
                }).ToList() ?? new List<EnsembleResult>()
            };

            var metadata = new Dictionary<string, object?>
            {
                ["modelCount"] = result.ModelContributions?.Count ?? 0,
                ["taskType"] = request.TaskType ?? "classification",
                ["outputData"] = result.OutputData,
                ["confidenceScore"] = result.ConfidenceScore
            };

            return Ok(Success(response, metadata));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid ensemble request received");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<EnsembleResponse>(new[] { error }));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ensemble inference failed due to invalid operation");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<EnsembleResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ensemble inference failed unexpectedly");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred during ensemble inference.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<EnsembleResponse>(new[] { error }));
        }
    }
}
