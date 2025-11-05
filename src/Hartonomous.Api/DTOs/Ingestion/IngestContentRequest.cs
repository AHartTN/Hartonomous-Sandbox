using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Ingestion;

public class IngestContentRequest
{
    [Required]
    public required string Modality { get; set; } // "text", "image", "audio", "video", "scada", "model"

    public string? Subtype { get; set; }

    [Required]
    public required string ContentHash { get; set; }

    public string? SourceUri { get; set; }
    public string? SourceType { get; set; }
    public string? CanonicalText { get; set; }

    public byte[]? RawContent { get; set; }
    public string? ContentBase64 { get; set; }

    public float[]? Embedding { get; set; }
    public string? EmbeddingType { get; set; }
    public int? ModelId { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
    public Dictionary<string, float>? Components { get; set; }

    public string? DeduplicationPolicy { get; set; }
}

public class IngestContentResponse
{
    public long AtomId { get; set; }
    public bool WasDuplicate { get; set; }
    public string? DuplicateReason { get; set; }
    public double? SemanticSimilarity { get; set; }
    public long? EmbeddingId { get; set; }
    public int ActualDimension { get; set; }
    public bool UsedPadding { get; set; }
}
