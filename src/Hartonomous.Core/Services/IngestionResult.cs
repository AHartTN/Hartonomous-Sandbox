namespace Hartonomous.Core.Services;

/// <summary>
/// Result of an ingestion operation.
/// </summary>
public class IngestionResult
{
    public bool Success { get; set; }
    public int ItemsProcessed { get; set; }
    public string Message { get; set; } = string.Empty;
}
