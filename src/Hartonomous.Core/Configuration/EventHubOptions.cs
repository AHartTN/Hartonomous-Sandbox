namespace Hartonomous.Core.Configuration;

/// <summary>
/// Configuration options for Azure Event Hubs connectivity
/// </summary>
public sealed class EventHubOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "EventHub";

    /// <summary>
    /// Event Hub connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Event Hub name/path
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Consumer group (for consumers only)
    /// </summary>
    public string ConsumerGroup { get; set; } = "$Default";

    /// <summary>
    /// Blob storage connection string for checkpointing (consumers only)
    /// </summary>
    public string? BlobStorageConnectionString { get; set; }

    /// <summary>
    /// Blob container name for checkpoints
    /// </summary>
    public string BlobContainerName { get; set; } = "eventhub-checkpoints";

    /// <summary>
    /// Maximum batch size for publishing
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
