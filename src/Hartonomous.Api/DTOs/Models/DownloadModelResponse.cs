namespace Hartonomous.Api.DTOs.Models;

/// <summary>
/// Response after downloading a model from HuggingFace or Ollama.
/// </summary>
public sealed class DownloadModelResponse
{
    /// <summary>
    /// Database model ID (only set if ingested).
    /// </summary>
    public int? ModelId { get; set; }

    /// <summary>
    /// Local cache path where model was downloaded.
    /// </summary>
    public string CachePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether the model was ingested into the database.
    /// </summary>
    public bool Ingested { get; set; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
