using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Hartonomous.Api.DTOs.Ingestion;

public class IngestFileRequest
{
    [Required]
    public required IFormFile File { get; set; }

    [Required]
    public required string Modality { get; set; } // "text", "image", "audio", "video", "scada", "model"

    public string? Subtype { get; set; }

    public string? SourceUri { get; set; }

    public string? SourceType { get; set; }

    public string? EmbeddingType { get; set; }

    public int? ModelId { get; set; }

    public string? DeduplicationPolicy { get; set; }
}
