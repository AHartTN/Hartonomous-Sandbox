using Microsoft.AspNetCore.Http;

namespace Hartonomous.Api.DTOs;

/// <summary>
/// Request model for model file ingestion.
/// </summary>
public record ModelIngestRequest(
    /// <summary>
    /// Model file to ingest (supports Safetensors, ONNX, PyTorch, GGUF).
    /// </summary>
    IFormFile ModelFile,

    /// <summary>
    /// Optional friendly name for the model.
    /// </summary>
    string? ModelName = null,

    /// <summary>
    /// Optional architecture type (e.g., "transformer", "cnn").
    /// </summary>
    string? Architecture = null
);
