using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Bulk;

public class BulkContentItem
{
    [Required]
    public required string Modality { get; set; }

    public string? CanonicalText { get; set; }

    public string? BinaryDataBase64 { get; set; }

    public string? ContentUrl { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
}
