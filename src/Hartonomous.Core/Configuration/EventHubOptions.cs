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
    /// Optional legacy blob checkpoint connection string. Retained for backwards compatibility.
    /// </summary>
    public string? BlobStorageConnectionString { get; set; }

    /// <summary>
    /// Name of the SQL table that stores Event Hub checkpoints. Defaults to dbo.EventHubCheckpoints.
    /// </summary>
    public string CheckpointTableName { get; set; } = "EventHubCheckpoints";

    /// <summary>
    /// Maximum batch size for publishing
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Retry attempts for failed operations
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
}
