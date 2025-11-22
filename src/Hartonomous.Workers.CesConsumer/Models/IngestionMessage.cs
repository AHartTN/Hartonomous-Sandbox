namespace Hartonomous.Workers.CesConsumer.Models;

/// <summary>
/// Represents a message from the Service Broker IngestionQueue
/// </summary>
public class IngestionMessage
{
    public byte[]? FileData { get; set; }
    public string? FileName { get; set; }
    public int TenantId { get; set; }
}
