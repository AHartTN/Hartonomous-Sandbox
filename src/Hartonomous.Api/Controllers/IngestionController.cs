using Hartonomous.Api.Common;
using Hartonomous.Api.DTOs.Ingestion;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IAtomIngestionService _ingestionService;
    private readonly ILogger<IngestionController> _logger;

    public IngestionController(
        IAtomIngestionService ingestionService,
        ILogger<IngestionController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [HttpPost("content")]
    [ProducesResponseType(typeof(ApiResponse<IngestContentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> IngestContent(
        [FromBody] IngestContentRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Request body is required"));
        }

        if (string.IsNullOrWhiteSpace(request.Modality))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "Modality is required"));
        }

        try
        {
            _logger.LogInformation("Ingesting {Modality} content from {Source}", 
                request.Modality, 
                request.SourceUri ?? "direct upload");

            var ingestionRequest = new AtomIngestionRequest
            {
                Modality = request.Modality,
                Subtype = request.Subtype,
                HashInput = request.ContentHash,
                SourceUri = request.SourceUri,
                SourceType = request.SourceType,
                CanonicalText = request.CanonicalText,
                Embedding = request.Embedding,
                EmbeddingType = request.EmbeddingType ?? "default",
                ModelId = request.ModelId,
                Metadata = request.Metadata != null 
                    ? System.Text.Json.JsonSerializer.Serialize(request.Metadata) 
                    : null,
                Components = request.Components != null
                    ? request.Components.Select(kvp => new AtomComponentDescriptor((long)kvp.Value, 1)).ToList()
                    : Array.Empty<AtomComponentDescriptor>(),
                PolicyName = request.DeduplicationPolicy ?? "hash"
            };

            var result = await _ingestionService.IngestAsync(ingestionRequest, cancellationToken);

            var response = new IngestContentResponse
            {
                AtomId = result.Atom.AtomId,
                WasDuplicate = result.WasDuplicate,
                DuplicateReason = result.DuplicateReason,
                SemanticSimilarity = result.SemanticSimilarity,
                EmbeddingId = result.Embedding?.AtomEmbeddingId,
                ActualDimension = result.Embedding?.Dimension ?? 0,
                UsedPadding = false // Property removed from schema
            };

            _logger.LogInformation(
                "Ingestion completed: AtomId={AtomId}, Deduped={Deduped}, Similarity={Similarity:F3}", 
                result.Atom.AtomId, 
                result.WasDuplicate,
                result.SemanticSimilarity ?? 0);

            var metadata = new Dictionary<string, object>
            {
                ["modality"] = request.Modality,
                ["deduplicationSavings"] = result.WasDuplicate ? "100%" : "0%",
                ["semanticSimilarity"] = result.SemanticSimilarity ?? 0
            };

            return Ok(ApiResponse<IngestContentResponse>.Ok(response, new ApiMetadata 
            { 
                Extra = metadata 
            }));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid ingestion request for {Modality}", request.Modality);
            return BadRequest(ApiResponse<object>.Fail("VALIDATION_ERROR", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Content ingestion failed due to invalid operation for {Modality}", request.Modality);
            return StatusCode(500, ApiResponse<object>.Fail("INGESTION_FAILED", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during {Modality} ingestion", request.Modality);
            return StatusCode(500, ApiResponse<object>.Fail("INGESTION_FAILED", "An unexpected error occurred during ingestion", ex.Message));
        }
    }

    [HttpPost("file")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<IngestContentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> IngestFile(
        [FromForm] IngestFileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "File is required."));
        }

        try
        {
            _logger.LogInformation("Ingesting file {FileName} ({Size} bytes) with modality {Modality}", 
                request.File.FileName, request.File.Length, request.Modality);

            using var memoryStream = new MemoryStream();
            await request.File.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            // Calculate content hash
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var contentHash = sha256.ComputeHash(fileBytes);
            var contentHashString = Convert.ToBase64String(contentHash);

            var ingestionRequest = new AtomIngestionRequest
            {
                Modality = request.Modality,
                Subtype = request.Subtype,
                HashInput = contentHashString,
                SourceUri = request.SourceUri ?? $"file://{request.File.FileName}",
                SourceType = request.SourceType ?? "file-upload",
                EmbeddingType = request.EmbeddingType ?? "default",
                ModelId = request.ModelId,
                PolicyName = request.DeduplicationPolicy ?? "default"
            };

            var result = await _ingestionService.IngestAsync(ingestionRequest, cancellationToken);

            var response = new IngestContentResponse
            {
                AtomId = result.Atom.AtomId,
                WasDuplicate = result.WasDuplicate,
                DuplicateReason = result.DuplicateReason,
                SemanticSimilarity = result.SemanticSimilarity
            };

            return Ok(ApiResponse<IngestContentResponse>.Ok(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file ingestion for {FileName}", request.File.FileName);
            return StatusCode(500, ApiResponse<object>.Fail("INGESTION_FAILED", "An unexpected error occurred during file ingestion.", ex.Message));
        }
    }
}
