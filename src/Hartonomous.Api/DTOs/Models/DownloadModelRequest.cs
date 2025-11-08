namespace Hartonomous.Api.DTOs.Models;

/// <summary>
/// Request for downloading a model from HuggingFace or Ollama.
/// </summary>
public sealed class DownloadModelRequest
{
    /// <summary>
    /// Model identifier (HuggingFace: "org/model-name", Ollama: "model:tag").
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Optional friendly name for the model (used during ingestion).
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// Whether to automatically ingest the model after downloading (default: true).
    /// </summary>
    public bool IngestAfterDownload { get; set; } = true;
}
