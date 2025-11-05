using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Hartonomous.Api.DTOs;

public class ModelIngestRequest
{
    [Required]
    public required string ModelName { get; set; }
    
    public string? ModelFormat { get; set; }
    public string? Description { get; set; }
    
    [Required]
    public required IFormFile ModelFile { get; set; }
}
