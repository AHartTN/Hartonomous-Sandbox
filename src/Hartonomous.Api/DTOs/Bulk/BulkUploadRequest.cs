using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Bulk;

public class BulkUploadRequest
{
    [Required]
    public required string Modality { get; set; }

    public int? ModelId { get; set; }

    public bool ExtractMetadata { get; set; } = true;

    public bool EnableDeduplication { get; set; } = true;

    public Dictionary<string, string>? GlobalMetadata { get; set; }
}
