namespace Hartonomous.Api.DTOs.Bulk;

public class BulkUploadResponse
{
    public required string JobId { get; set; }
    public int FilesReceived { get; set; }
    public long TotalBytes { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
