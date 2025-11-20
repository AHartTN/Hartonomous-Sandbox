namespace Hartonomous.Api.DTOs.Ingestion;

public class UrlIngestionRequest
{
    public required string Url { get; set; }
    public int TenantId { get; set; } = 0;
}
