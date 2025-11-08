using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Bulk;

public class BulkIngestRequest
{
    [Required]
    public required List<BulkContentItem> Items { get; set; }

    public int? ModelId { get; set; }

    public bool ProcessAsync { get; set; } = true;

    public string? CallbackUrl { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}
