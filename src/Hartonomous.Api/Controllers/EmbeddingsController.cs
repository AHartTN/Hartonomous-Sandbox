using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Hartonomous.Api.DTOs;
using Hartonomous.Core.Interfaces;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hartonomous.Api.Controllers;

[Route("api/embeddings")]
public sealed class EmbeddingsController : ApiControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IAtomIngestionService _atomIngestionService;
    private readonly ILogger<EmbeddingsController> _logger;

    public EmbeddingsController(
        IEmbeddingService embeddingService,
        IAtomIngestionService atomIngestionService,
        ILogger<EmbeddingsController> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _atomIngestionService = atomIngestionService ?? throw new ArgumentNullException(nameof(atomIngestionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("text")]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<EmbeddingResponse>>> CreateTextEmbeddingAsync(
        [FromBody] EmbeddingRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<EmbeddingResponse>(new[] { ValidationError("Request body is required.") }));
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(Failure<EmbeddingResponse>(new[] { MissingField(nameof(request.Text)) }));
        }

        try
        {
            var embeddingVector = await _embeddingService
                .EmbedTextAsync(request.Text, cancellationToken)
                .ConfigureAwait(false);

            var embeddingLabel = string.IsNullOrWhiteSpace(request.EmbeddingType)
                ? "text"
                : request.EmbeddingType.Trim();

            var ingestionRequest = new AtomIngestionRequest
            {
                HashInput = $"text:{request.Text}",
                Modality = "text",
                Subtype = embeddingLabel,
                SourceType = embeddingLabel,
                CanonicalText = request.Text,
                Embedding = embeddingVector,
                EmbeddingType = embeddingLabel,
                ModelId = request.ModelId,
                PolicyName = "default"
            };

            var ingestionResult = await _atomIngestionService
                .IngestAsync(ingestionRequest, cancellationToken)
                .ConfigureAwait(false);

            var response = new EmbeddingResponse(
                ingestionResult.Atom.AtomId,
                ingestionResult.Embedding?.AtomEmbeddingId,
                ingestionResult.WasDuplicate,
                ingestionResult.DuplicateReason,
                ingestionResult.SemanticSimilarity);

            var metadata = request.ModelId.HasValue
                ? new Dictionary<string, object?>
                {
                    ["modelId"] = request.ModelId.Value,
                    ["embeddingDimension"] = embeddingVector.Length
                }
                : new Dictionary<string, object?>
                {
                    ["embeddingDimension"] = embeddingVector.Length
                };

            return Ok(Success(response, metadata));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid embedding request for text input.");
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<EmbeddingResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create embedding for text input.");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while creating the embedding.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<EmbeddingResponse>(new[] { error }));
        }
    }

    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponse<EmbeddingResponse>>> CreateImageEmbeddingAsync(
        [FromForm] MediaEmbeddingRequest request,
        CancellationToken cancellationToken)
        => HandleMediaEmbeddingAsync(request, _embeddingService.EmbedImageAsync, "image", cancellationToken);

    [HttpPost("audio")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponse<EmbeddingResponse>>> CreateAudioEmbeddingAsync(
        [FromForm] MediaEmbeddingRequest request,
        CancellationToken cancellationToken)
        => HandleMediaEmbeddingAsync(request, _embeddingService.EmbedAudioAsync, "audio", cancellationToken);

    [HttpPost("video-frame")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingResponse>), StatusCodes.Status500InternalServerError)]
    public Task<ActionResult<ApiResponse<EmbeddingResponse>>> CreateVideoFrameEmbeddingAsync(
        [FromForm] MediaEmbeddingRequest request,
        CancellationToken cancellationToken)
        => HandleMediaEmbeddingAsync(request, _embeddingService.EmbedVideoFrameAsync, "video_frame", cancellationToken);

    private async Task<ActionResult<ApiResponse<EmbeddingResponse>>> HandleMediaEmbeddingAsync(
        MediaEmbeddingRequest? request,
        Func<byte[], CancellationToken, Task<float[]>> embedFunc,
        string modality,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(Failure<EmbeddingResponse>(new[] { ValidationError("Form data is required.") }));
        }

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(Failure<EmbeddingResponse>(new[] { MissingField(nameof(request.File)) }));
        }

        if (request.File.Length > 64 * 1024 * 1024)
        {
            var error = ErrorDetailFactory.Validation("Payload exceeds 64 MB limit.", nameof(request.File));
            return BadRequest(Failure<EmbeddingResponse>(new[] { error }));
        }

        try
        {
            await using var memoryStream = new MemoryStream((int)request.File.Length);
            await request.File.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            var payload = memoryStream.ToArray();

            var embeddingVector = await embedFunc(payload, cancellationToken).ConfigureAwait(false);

            var sourceType = string.IsNullOrWhiteSpace(request.SourceType) ? modality : request.SourceType.Trim();
            var fileName = string.IsNullOrWhiteSpace(request.File.FileName) ? "upload" : request.File.FileName;
            var payloadHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

            var ingestionRequest = new AtomIngestionRequest
            {
                HashInput = $"{modality}:{payloadHash}",
                Modality = modality,
                Subtype = sourceType,
                SourceType = sourceType,
                CanonicalText = $"{modality}:{fileName}",
                Metadata = request.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(request.Metadata) : null,
                Embedding = embeddingVector,
                EmbeddingType = sourceType,
                ModelId = request.ModelId,
                PolicyName = "default"
            };

            var ingestionResult = await _atomIngestionService
                .IngestAsync(ingestionRequest, cancellationToken)
                .ConfigureAwait(false);

            var response = new EmbeddingResponse(
                ingestionResult.Atom.AtomId,
                ingestionResult.Embedding?.AtomEmbeddingId,
                ingestionResult.WasDuplicate,
                ingestionResult.DuplicateReason,
                ingestionResult.SemanticSimilarity);

            var metadata = new Dictionary<string, object?>
            {
                ["embeddingDimension"] = embeddingVector.Length,
                ["mediaType"] = modality,
                ["fileName"] = fileName,
                ["contentType"] = request.File.ContentType,
                ["payloadBytes"] = payload.Length
            };

            if (request.ModelId.HasValue)
            {
                metadata["modelId"] = request.ModelId.Value;
            }

            if (request.Metadata != null && request.Metadata.Count > 0)
            {
                metadata["metadata"] = System.Text.Json.JsonSerializer.Serialize(request.Metadata);
            }

            return Ok(Success(response, metadata));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid {Modality} embedding request received.", modality);
            var error = ErrorDetailFactory.Validation(ex.Message);
            return BadRequest(Failure<EmbeddingResponse>(new[] { error }));
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "I/O failure while reading {Modality} payload.", modality);
            var error = ErrorDetailFactory.Validation("The uploaded file could not be read.", nameof(request.File));
            return BadRequest(Failure<EmbeddingResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create embedding for {Modality} payload.", modality);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while creating the embedding.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<EmbeddingResponse>(new[] { error }));
        }
    }
}
