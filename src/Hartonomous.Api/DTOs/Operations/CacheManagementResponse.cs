namespace Hartonomous.Api.DTOs.Operations;

public class CacheManagementResponse
{
    public required string Operation { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public CacheStats? Stats { get; set; }
}
