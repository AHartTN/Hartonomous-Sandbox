using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs;

public class EmbeddingRequest
{
    [Required]
    public required string Text { get; set; }
    
    public int? ModelId { get; set; }
    public string? EmbeddingType { get; set; }
}
