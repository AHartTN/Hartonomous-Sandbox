namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Statistics for model ingestion operations.
/// </summary>
public class IngestionStats
{
    public int TotalModels { get; set; }
    public long TotalParameters { get; set; }
    public long TotalLayers { get; set; }
    public Dictionary<string, int> ArchitectureBreakdown { get; set; } = new();
}
