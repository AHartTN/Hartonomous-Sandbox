using Microsoft.AspNetCore.Http;

namespace Hartonomous.Api.DTOs;

public class MediaEmbeddingRequest
{
    public string? Url { get; set; }
    public string? Base64Data { get; set; }
    public IFormFile? File { get; set; }
    public int? ModelId { get; set; }
    public string? SourceType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
